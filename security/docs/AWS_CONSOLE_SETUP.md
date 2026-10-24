# AWS setup checklist for ATTT + SE (manual console review; not a deployment script)

## 0. Account and expense control
- Owner creates a separate development AWS account or isolated dev resources.
- Configure MFA and AWS IAM Identity Center; use permission sets for each contributor.
- Create AWS Budgets cost alerts (monitoring is not a hard service spending cap). Choose a Region supported by chosen Bedrock model and other services; verify data handling and pricing.

## 1. S3 intake
1. S3 > Buckets > Create a private bucket for contracts in chosen Region.
2. Block Public Access = ON, Object Ownership = Bucket owner enforced (ACL disabled), default encryption = SSE-KMS where required and tested.
3. Enable versioning on buckets containing originals/evidence when retention requires it.
4. Plan `quarantine/`, `clean/`, `analysis/` prefixes (or separate buckets for stronger controls). Users may PUT only a *specific* generated quarantine object through short-lived presigned URL.
5. Create bucket policy enforcing TLS and non-public access; consider `s3:signatureAge` and checksums.
6. Do not rely on extensions/MIME only; validate magic bytes and parser limits. The scanner must be the ONLY principal allowed to change quarantine objects to clean status.

## 2. Malware scanning
Option A (AWS-managed): GuardDuty > Malware Protection for S3 > Enable for new objects under `quarantine/`; configure dedicated IAM role, EventBridge results, optional tag `GuardDutyMalwareScanStatus`. Move to clean only when `NO_THREATS_FOUND` and other validation passes. `UNSUPPORTED`, `ACCESS_DENIED`, `FAILED`, or scan timeout remain quarantined. S3 tags alone do not prove authenticity; validate object version and events. Verify pricing and service availability.
Option B (lab): isolated ClamAV scanner container, signature updates and fail-closed state. Do NOT run ClamAV inside the AI worker that parses all files without separation.

## 3. SQS
- `analysis-requests` and `analysis-results`, each with a DLQ.
- Define visibility timeout based on actual workload, extend while processing, set maxReceiveCount after testing (e.g., 5 is only a starting point).
- Use idempotent backend consumer and reject out-of-order job transitions. Message metadata MUST resolve to canonical backend-owned job and tenant.
- Limit queue resource policies to relevant roles; encryption at rest with managed SQS encryption or KMS per policy.

## 4. IAM and ECS
- Separate API task role, scanner role, worker task role, and ECS task EXECUTION role.
- `aws/iam/worker-task-policy.TEMPLATE.json` is illustrative; replace every `REPLACE_*` and validate with Access Analyzer.
- Grant worker clean object read, analysis results write, request queue consume, results queue send, narrowly approved Bedrock model, necessary KMS permissions. Do not give worker DB credentials or DDL permissions.
- Review execution role separately for ECR, logs and secrets as needed.
- Put RDS in isolated private subnet and restrict inbound DB port only from backend task security group. ECS/Fargate networking and outbound AWS endpoints must be planned, especially with a no-NAT design.

## 5. Cognito and API
- Cognito > User pools > Create; register SPA as public client with **no client secret** and authorization code grant + PKCE.
- Configure exact callback and sign-out URLs for DEV/TEST (localhost) then PROD, allowed scopes, and secure token handling.
- ASP.NET validates JWT signature, issuer, audience/token use/expiry; checks *resource-level* tenant membership and RBAC on every endpoint.
- Never trust tenant_id, user role, contract_id, job_id merely because they're in the request body.

## 6. Observability
- CloudTrail management events for control-plane changes; enable specific S3 data events if object-level auditing needed and budgeted.
- CloudWatch alarms for repeated auth failures, security rejects, stalled jobs, DLQs, malware, denied cross-tenant access.
- Correlate events by job_id; sanitize logs so contract text/PII/tokens and presigned URLs are not stored inadvertently.

## 7. Deployment gate
- ATTT approval for IAM+S3+RBAC, validation of scan gating, AI security tests and threat model.
- SE runs `cdk synth`, peer reviews CloudFormation diffs and checks IaC with Checkov, then deploys under dev role.
- Production needs privacy, legal, data residency and contractual review beyond this sample.
