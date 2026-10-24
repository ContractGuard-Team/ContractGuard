# ContractGuard Security Integration Kit — ATTT

This is a **runnable, offline security-rule reference prototype** for a .NET 10 + Python/Bedrock + AWS ContractGuard team. It does **NOT** deploy AWS infrastructure, train an AI model, validate full legal compliance, or replace C# production risk-scoring logic.

## Quick start (Windows PowerShell)

```powershell
py -3.12 -m venv .venv
.\.venv\Scripts\Activate.ps1
python -m pip install -r requirements.txt
python -m pytest -q
python -m src.rule_validator --input fixtures/mixed.json --output out/security-findings.json
```

## What's included
- `rules/cyber_rules.yaml`: 20 sample cyber/privacy/third-party checks; all context-dependent
- `policies/demo_saas_policy.yaml`: **FICTIONAL** B2B SaaS buyer security policy (e.g., 24-hour incident notice is a demonstration, not universal law)
- `schemas/extraction.schema.json`: AI → backend contract
- `schemas/security-findings.schema.json`: sample reference validation output
- `src/rule_validator.py`: runnable offline Python reference checker only
- `fixtures/`: sample structured outputs for regression
- `tests/`: 9 tests for policy checks and schema
- `aws/iam/`: non-deployable least-privilege template
- `docs/`: ownership, integration procedure, threat model, test plan
- `ai-security/`: safe synthetic prompt injection test excerpt
- `.github/workflows/`: GitHub Actions template: **move this workflow into monorepo root `.github/workflows/`** when integrated

## Group roles
- AI: produce source-grounded extraction JSON and clause IDs; do not let LLM manufacture tenant/job identifiers.
- ATTT: define risks, applicability, security policy, rules, controls, negative tests and review results.
- SE: implement the authoritative deterministic **C#** rule evaluator, job binding, persistence and AWS/CDK deployments.

Read `docs/WINDOWS_SETUP.md`, `docs/AWS_CONSOLE_SETUP.md` and `docs/HANDOFF_CHECKLIST.md` before integrating.

## Important caveats
1. This checker verifies only schema/evidence field presence, **not** whether cited quotes actually occur in the canonical document. The group must implement content/source verification.
2. Missing facts always become `NEEDS_REVIEW` in this MVP, not automatically policy FAIL or legal violation.
3. No runtime secrets or real contracts go into Git. Use a temporary-credentials SSO profile and AWS Secrets Manager where appropriate.
4. IAM sample contains `REPLACE_*` placeholders and is NOT safe to deploy without design review, especially KMS/key policies/model ARNs.
5. AI in a production worker must **not** write business database. Backend owns tenant authorization and scoring.

## References
- NIST CSF 2.0 https://doi.org/10.6028/NIST.CSWP.29
- NIST SP 1305 https://csrc.nist.gov/pubs/sp/1305/final
- NIST SP 800-161 https://csrc.nist.gov/pubs/sp/800/161/r1/upd1/final
- OWASP LLM Top 10 https://genai.owasp.org/llm-top-10/
- OWASP File Upload https://cheatsheetseries.owasp.org/cheatsheets/File_Upload_Cheat_Sheet.html
- AWS CLI SSO https://docs.aws.amazon.com/cli/latest/userguide/cli-configure-sso.html
- AWS S3 presigned URL https://docs.aws.amazon.com/AmazonS3/latest/userguide/using-presigned-url.html
- AWS ECS task IAM https://docs.aws.amazon.com/AmazonECS/latest/developerguide/security-iam-roles.html
