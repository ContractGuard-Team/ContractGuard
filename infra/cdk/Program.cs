using Amazon.CDK;
using ContractGuard.Infra;

var app = new App();

// Production only, up for ~3 days around the showcase; staging runs on Railway (docs/04 C7).
_ = new AppStack(app, "ContractGuard-Production", "production", new StackProps
{
    Env = new Amazon.CDK.Environment { Account = "699061129385", Region = "ap-southeast-1" },
    Description = "ContractGuard production: SPA on CloudFront + S3, API on ECS Fargate behind an internal ALB",
});

app.Synth();
