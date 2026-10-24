# IAM baseline (non-deployable template)

* Do NOT deploy `worker-task-policy.TEMPLATE.json` verbatim. Replace bucket, queue, account, region, approved model/inference-profile ARNs, and KMS key ARN. Scope KMS permissions and key policy to the operations required; `kms:GenerateDataKey` may not be needed for every role.
* Separate ECS **task role** (runtime S3/SQS/Bedrock/KMS) from ECS **task execution role** (pull image, emit logs, read injected secrets where required).
* API role, scanner role, and worker role MUST be different. API role should not have Bedrock invocation access by default. Scanner must never publish clean status without finishing validation.
* Cognito end-user authorization belongs in ASP.NET Core; AWS IAM is service-to-service authorization.
* Add permissions for observability and related calls based on actual SDK usage. Review with AWS IAM Access Analyzer before deployment.
* Use `aws configure sso` with temporary credentials; never commit `~/.aws/config` or SSO cache.
* Ensure AWS endpoint/VPC architecture is validated by Dev/ATTT; this package does not deploy infrastructure.
