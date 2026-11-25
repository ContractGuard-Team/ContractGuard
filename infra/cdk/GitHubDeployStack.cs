using Amazon.CDK;
using Amazon.CDK.AWS.IAM;
using Constructs;

namespace ContractGuard.Infra;

// Lets the manual workflow .github/workflows/deploy-staging-aws.yml sign in to AWS with GitHub OIDC, so no
// access key is stored on GitHub. Deployed once by hand; apart from AppStack so `cdk destroy` of the demo keeps it.
public sealed class GitHubDeployStack : Stack
{
    public const string RoleName = "contractguard-github-deploy";

    public GitHubDeployStack(Construct scope, string id, IStackProps props) : base(scope, id, props)
    {
        var github = new OidcProviderNative(this, "GitHub", new OidcProviderNativeProps
        {
            Url = "https://token.actions.githubusercontent.com",
            ClientIds = ["sts.amazonaws.com"],
        });

        // Only runs on main of the team repo can take the role, not branches or forks.
        var role = new Role(this, "DeployRole", new RoleProps
        {
            RoleName = RoleName,
            MaxSessionDuration = Duration.Hours(1),
            AssumedBy = new WebIdentityPrincipal(github.OidcProviderArn, new Dictionary<string, object>
            {
                ["StringEquals"] = new Dictionary<string, string>
                {
                    ["token.actions.githubusercontent.com:aud"] = "sts.amazonaws.com",
                    ["token.actions.githubusercontent.com:sub"] = "repo:ContractGuard-Team/ContractGuard:ref:refs/heads/main",
                },
            }),
        });

        // `cdk deploy` does its work through the bootstrap roles; the migration step reads the database secret.
        role.AddToPolicy(new PolicyStatement(new PolicyStatementProps
        {
            Actions = ["sts:AssumeRole"],
            Resources = [$"arn:aws:iam::{Account}:role/cdk-hnb659fds-*"],
        }));
        role.AddToPolicy(new PolicyStatement(new PolicyStatementProps
        {
            Actions = ["secretsmanager:GetSecretValue"],
            Resources = [$"arn:aws:secretsmanager:{Region}:{Account}:secret:contractguard/staging-aws/database-*"],
        }));

        _ = new CfnOutput(this, "RoleArn", new CfnOutputProps { Value = role.RoleArn });
    }
}
