using Amazon.CDK;
using Amazon.CDK.AWS.CloudFront;
using Amazon.CDK.AWS.CloudFront.Origins;
using Amazon.CDK.AWS.EC2;
using Amazon.CDK.AWS.Ecr.Assets;
using Amazon.CDK.AWS.ECS;
using Amazon.CDK.AWS.ElasticLoadBalancingV2;
using Amazon.CDK.AWS.IAM;
using Amazon.CDK.AWS.Logs;
using Amazon.CDK.AWS.S3;
using Amazon.CDK.AWS.S3.Deployment;
using Amazon.CDK.AWS.SQS;
using Constructs;
using CfFunction = Amazon.CDK.AWS.CloudFront.Function;
using EcsSecret = Amazon.CDK.AWS.ECS.Secret;
using SmSecret = Amazon.CDK.AWS.SecretsManager.Secret;

namespace ContractGuard.Infra;

// One environment of ContractGuard (SD-02, SD-03, SD-07, docs/04 C1–C7). The database is Supabase,
// outside AWS; the AI worker and model run in the AI team's account (C2, C3).
public sealed class AppStack : Stack
{
    // Paths are relative to infra/cdk, where `cdk` runs.
    private const string RepoRoot = "../..";
    private const string SpaBuild = "../../frontend/build/client";

    // docs/07 §5: the AI team's worker, on EC2 in their account.
    private const string AiWorkerRole = "arn:aws:iam::333596350642:role/ContractRisk-EC2-Role";

    // docs/04 C5: an SES email identity, verified by hand (no domain yet).
    private const string EmailSender = "contractguard.info@gmail.com";

    public AppStack(Construct scope, string id, string stage, IStackProps props) : base(scope, id, props)
    {
        // No NAT gateway (~$40/month): API tasks sit in public subnets with a public IP to reach Supabase,
        // ECR and SES, and their security group only lets the ALB in. The ALB sits in isolated subnets,
        // where only CloudFront reaches it, through a VPC origin.
        var vpc = new Vpc(this, "Vpc", new VpcProps
        {
            MaxAzs = 2,
            NatGateways = 0,
            SubnetConfiguration =
            [
                new SubnetConfiguration { Name = "public", SubnetType = SubnetType.PUBLIC, CidrMask = 24 },
                new SubnetConfiguration { Name = "internal", SubnetType = SubnetType.PRIVATE_ISOLATED, CidrMask = 24 },
            ],
        });

        var alb = new ApplicationLoadBalancer(this, "Alb", new ApplicationLoadBalancerProps
        {
            Vpc = vpc,
            InternetFacing = false,
            VpcSubnets = new SubnetSelection { SubnetType = SubnetType.PRIVATE_ISOLATED },
        });
        var cloudFront = PrefixList.FromLookup(this, "CloudFrontOriginFacing", new PrefixListLookupOptions
        {
            PrefixListName = "com.amazonaws.global.cloudfront.origin-facing",
        });
        alb.Connections.AllowFrom(cloudFront, Port.Tcp(80), "CloudFront VPC origin");
        // shortcut: http listener until the team's domain and its ACM certificate exist (docs/04 C7). With http
        // the ALB sends X-Forwarded-Proto: http and the API's Secure cookies fail, so add the https listener
        // (and HTTPS_ONLY on the VPC origin) before the first production deploy.
        var listener = alb.AddListener("Http", new BaseApplicationListenerProps { Port = 80, Open = false });

        var site = new Bucket(this, "Site", new BucketProps
        {
            BlockPublicAccess = BlockPublicAccess.BLOCK_ALL,
            EnforceSSL = true,
            Encryption = BucketEncryption.S3_MANAGED,
        });

        // Client-side routes (/contracts/123) are not files: CloudFront answers them with the app shell.
        // A distribution-wide 403/404 error page would also replace the API's own 404 and 403 answers.
        var spaRouting = new CfFunction(this, "SpaRouting", new FunctionProps
        {
            Runtime = FunctionRuntime.JS_2_0,
            Code = FunctionCode.FromInline("""
                function handler(event) {
                  var request = event.request;
                  if (request.uri.indexOf('.') === -1) {
                    request.uri = '/index.html';
                  }
                  return request;
                }
                """),
        });

        var distribution = new Amazon.CDK.AWS.CloudFront.Distribution(this, "Web", new DistributionProps
        {
            Comment = $"ContractGuard {stage}",
            DefaultRootObject = "index.html",
            PriceClass = PriceClass.PRICE_CLASS_200,
            DefaultBehavior = new BehaviorOptions
            {
                Origin = S3BucketOrigin.WithOriginAccessControl(site),
                ViewerProtocolPolicy = ViewerProtocolPolicy.REDIRECT_TO_HTTPS,
                FunctionAssociations =
                [
                    new FunctionAssociation { Function = spaRouting, EventType = FunctionEventType.VIEWER_REQUEST },
                ],
            },
            // One origin for the browser (SD-03): the session cookie needs no CORS.
            AdditionalBehaviors = new Dictionary<string, IBehaviorOptions>
            {
                ["/api/*"] = new BehaviorOptions
                {
                    Origin = Amazon.CDK.AWS.CloudFront.Origins.VpcOrigin_.WithApplicationLoadBalancer(alb, new VpcOriginWithEndpointProps
                    {
                        ProtocolPolicy = OriginProtocolPolicy.HTTP_ONLY,
                    }),
                    ViewerProtocolPolicy = ViewerProtocolPolicy.HTTPS_ONLY,
                    AllowedMethods = AllowedMethods.ALLOW_ALL,
                    CachePolicy = CachePolicy.CACHING_DISABLED,
                    // Host (Google builds its redirect_uri from it), cookies and query reach the API.
                    OriginRequestPolicy = OriginRequestPolicy.ALL_VIEWER_AND_CLOUDFRONT_2022,
                },
            },
        });
        var appUrl = $"https://{distribution.DistributionDomainName}";

        // Hashed file names never go stale; index.html and the rest are checked on every load, so after a
        // deploy no browser runs the previous build against the new API. Old assets stay (prune off): a tab
        // opened before the deploy still loads its lazy chunks.
        _ = new BucketDeployment(this, "SiteAssets", new BucketDeploymentProps
        {
            Sources = [Source.Asset($"{SpaBuild}/assets")],
            DestinationBucket = site,
            DestinationKeyPrefix = "assets/",
            CacheControl = [Amazon.CDK.AWS.S3.Deployment.CacheControl.FromString("public, max-age=31536000, immutable")],
            Prune = false,
        });
        _ = new BucketDeployment(this, "SiteShell", new BucketDeploymentProps
        {
            Sources = [Source.Asset(SpaBuild, new Amazon.CDK.AWS.S3.Assets.AssetOptions { Exclude = ["assets"] })],
            DestinationBucket = site,
            CacheControl = [Amazon.CDK.AWS.S3.Deployment.CacheControl.NoCache()],
            Prune = false,
            Distribution = distribution,
            DistributionPaths = ["/*"],
        });

        // SD-07: one bucket, prefixes quarantine/, clean/, results/. SSE-S3 rather than SSE-KMS: the AWS
        // managed KMS key cannot be used from the AI team's account. No versioning, so a delete is final.
        var documents = new Bucket(this, "Documents", new BucketProps
        {
            BlockPublicAccess = BlockPublicAccess.BLOCK_ALL,
            EnforceSSL = true,
            Encryption = BucketEncryption.S3_MANAGED,
            // The browser uploads with a presigned POST and the viewer reads with range requests.
            Cors =
            [
                new CorsRule
                {
                    AllowedOrigins = [appUrl],
                    AllowedMethods = [HttpMethods.POST, HttpMethods.GET, HttpMethods.HEAD],
                    AllowedHeaders = ["*"],
                    ExposedHeaders = ["Accept-Ranges", "Content-Length", "Content-Range"],
                },
            ],
        });

        var requests = NewQueue($"contractguard-{stage}-analysis-requests", visibilitySeconds: 900);
        var results = NewQueue($"contractguard-{stage}-analysis-results", visibilitySeconds: 60);

        // docs/07 §2, §5: the same rights as on the dev bucket and queues.
        var worker = new ArnPrincipal(AiWorkerRole);
        AllowWorker(documents, worker, ["s3:ListBucket"], documents.BucketArn);
        AllowWorker(documents, worker, ["s3:GetObject"], documents.ArnForObjects("quarantine/*"), documents.ArnForObjects("results/*/request.json"));
        AllowWorker(documents, worker, ["s3:PutObject"], documents.ArnForObjects("clean/*"), documents.ArnForObjects("results/*/extraction.json"));
        AllowWorker(documents, worker, ["s3:DeleteObject"], documents.ArnForObjects("quarantine/*"));
        requests.GrantConsumeMessages(worker);
        results.GrantSendMessages(worker);

        // Created by hand once, so the password never passes through CloudFormation (infra/cdk/README.md).
        var database = SmSecret.FromSecretNameV2(this, "Database", $"contractguard/{stage}/database");

        var logs = new LogGroup(this, "ApiLogs", new LogGroupProps
        {
            Retention = RetentionDays.TWO_WEEKS,
            RemovalPolicy = RemovalPolicy.DESTROY,
        });

        var task = new FargateTaskDefinition(this, "ApiTask", new FargateTaskDefinitionProps
        {
            Cpu = 512,
            MemoryLimitMiB = 1024,
            RuntimePlatform = new RuntimePlatform
            {
                CpuArchitecture = CpuArchitecture.ARM64,
                OperatingSystemFamily = OperatingSystemFamily.LINUX,
            },
        });
        task.AddContainer("api", new ContainerDefinitionOptions
        {
            Image = ContainerImage.FromAsset(RepoRoot, new AssetImageProps
            {
                File = "backend/Dockerfile",
                Target = "api",
                Platform = Platform_.LINUX_ARM64,
            }),
            PortMappings = [new PortMapping { ContainerPort = 8080 }],
            Logging = LogDriver.AwsLogs(new AwsLogDriverProps { StreamPrefix = "api", LogGroup = logs }),
            Environment = new Dictionary<string, string>
            {
                // Production: Secure cookies (the browser is on https) and no fake chat responder.
                ["ASPNETCORE_ENVIRONMENT"] = "Production",
                ["Storage__BucketName"] = documents.BucketName,
                ["Queues__AnalysisRequests"] = requests.QueueName,
                ["Queues__AnalysisResults"] = results.QueueName,
                ["Email__Provider"] = "ses",
                ["Email__FromAddress"] = EmailSender,
                ["Email__FrontendBaseUrl"] = appUrl,
                // Until the AI worker reads this environment's queue (docs/07): confirming a context only saves it.
                ["Analysis__WorkerEnabled"] = (string?)Node.TryGetContext("workerEnabled") ?? "false",
            },
            Secrets = new Dictionary<string, EcsSecret>
            {
                ["Database__ConnectionString"] = EcsSecret.FromSecretsManager(database),
            },
        });

        documents.GrantReadWrite(task.TaskRole);
        requests.GrantSendMessages(task.TaskRole);
        results.GrantConsumeMessages(task.TaskRole);
        task.TaskRole.AddToPrincipalPolicy(new PolicyStatement(new PolicyStatementProps
        {
            Actions = ["ses:SendEmail"],
            // In the SES sandbox the recipients are identities too, so the resource cannot be the sender alone.
            Resources = [$"arn:aws:ses:{Region}:{Account}:identity/*"],
            Conditions = new Dictionary<string, object>
            {
                ["StringEquals"] = new Dictionary<string, object> { ["ses:FromAddress"] = EmailSender },
            },
        }));

        var cluster = new Cluster(this, "Cluster", new ClusterProps { Vpc = vpc });
        var service = new FargateService(this, "Api", new FargateServiceProps
        {
            Cluster = cluster,
            TaskDefinition = task,
            // SD-20: `cdk deploy -c apiTasks=0` stops the API outside test hours.
            DesiredCount = int.Parse((string?)Node.TryGetContext("apiTasks") ?? "1"),
            AssignPublicIp = true,
            VpcSubnets = new SubnetSelection { SubnetType = SubnetType.PUBLIC },
            // A new task is healthy before the old one stops; a task that never gets healthy rolls back.
            MinHealthyPercent = 100,
            MaxHealthyPercent = 200,
            CircuitBreaker = new DeploymentCircuitBreaker { Rollback = true },
        });
        listener.AddTargets("Api", new AddApplicationTargetsProps
        {
            Port = 8080,
            Protocol = ApplicationProtocol.HTTP,
            Targets = [service],
            // Liveness, not readiness: a paused Supabase project must not make ECS restart the API forever.
            HealthCheck = new Amazon.CDK.AWS.ElasticLoadBalancingV2.HealthCheck { Path = "/health/live" },
            DeregistrationDelay = Duration.Seconds(15),
        });

        _ = new CfnOutput(this, "AppUrl", new CfnOutputProps { Value = appUrl });
        _ = new CfnOutput(this, "DocumentsBucket", new CfnOutputProps { Value = documents.BucketName });
        _ = new CfnOutput(this, "RequestsQueueUrl", new CfnOutputProps { Value = requests.QueueUrl });
        _ = new CfnOutput(this, "ResultsQueueUrl", new CfnOutputProps { Value = results.QueueUrl });
        _ = new CfnOutput(this, "ApiLogGroup", new CfnOutputProps { Value = logs.LogGroupName });
    }

    // Each queue has a dead-letter queue after 3 receives, like the local ones (infra/local/localstack).
    private Queue NewQueue(string name, int visibilitySeconds) => new(this, name, new QueueProps
    {
        QueueName = name,
        VisibilityTimeout = Duration.Seconds(visibilitySeconds),
        Encryption = QueueEncryption.SQS_MANAGED,
        EnforceSSL = true,
        DeadLetterQueue = new DeadLetterQueue
        {
            MaxReceiveCount = 3,
            Queue = new Queue(this, $"{name}-dlq", new QueueProps
            {
                QueueName = $"{name}-dlq",
                Encryption = QueueEncryption.SQS_MANAGED,
                EnforceSSL = true,
                RetentionPeriod = Duration.Days(14),
            }),
        },
    });

    private static void AllowWorker(Bucket bucket, IPrincipal worker, string[] actions, params string[] resources) =>
        bucket.AddToResourcePolicy(new PolicyStatement(new PolicyStatementProps
        {
            Principals = [worker],
            Actions = actions,
            Resources = resources,
        }));
}
