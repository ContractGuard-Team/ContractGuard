using Amazon.CDK;
using ContractGuard.Infra;

var app = new App();

// Staging on AWS for the demo, up for ~3 days around the showcase; the everyday staging runs on Railway
// (docs/04 C7, C9). Not production: no domain, SES in sandbox, database shared with the Railway staging.
_ = new AppStack(app, "ContractGuard-StagingAws", "staging-aws", new StackProps
{
    Env = new Amazon.CDK.Environment { Account = "699061129385", Region = "ap-southeast-1" },
    Description = "ContractGuard staging on AWS (demo): SPA on CloudFront + S3, API on ECS Fargate behind an internal ALB",
});

// The role the manual "Deploy: Staging (AWS)" workflow signs in with. Deployed once; it stays when the demo is destroyed.
_ = new GitHubDeployStack(app, "ContractGuard-GitHubDeploy", new StackProps
{
    Env = new Amazon.CDK.Environment { Account = "699061129385", Region = "ap-southeast-1" },
    Description = "ContractGuard: IAM role for GitHub Actions deploys (OIDC)",
});

app.Synth();
