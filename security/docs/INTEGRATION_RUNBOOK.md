# Integration runbook (not zero-configuration deployment)

## Windows development
```powershell
py -3.12 -m venv .venv
.\.venv\Scripts\Activate.ps1
python -m pip install -r requirements.txt
python -m pytest -q
python -m src.rule_validator --input fixtures/mixed.json --output out/security-findings.json
```

## Attach to group monorepo
1. Copy **this directory** as `security/` into canonical monorepo (do not duplicate `contracts/` schemas once integration finalized).
2. Freeze and version `schemas/extraction.schema.json` in shared `/contracts/` first, then update AI output and C# consumer together.
3. AI emits JSON to versioned S3 analysis key; C# reads and validates schema, job, tenant, source hash, provenance, and evidence.
4. C# reads versioned `policies/*.yaml` and `rules/*.yaml`; must implement `PASS`, `FAIL`, `NOT_APPLICABLE`, `NEEDS_REVIEW` identically to reference tests.
5. CI runs Python schema/reference tests and scans; merge only after review by ATTT+SE.
6. Test controlled E2E with a synthetic B2B SaaS contract; verify both detected failures and safe tenant boundaries.
7. Deployment to AWS only via reviewed CDK written by SE. Never deploy this repo's policy template with unresolved `REPLACE_*` values.

## Deployment gate
- IAM least privilege / no public buckets / encryption / logging configured
- SQS DLQ, backoff, idempotent C# consumer and poison-message handling
- C# confirms canonical job state; rejects cross-tenant and spoofed job IDs
- Secure file quarantine → scan → clean transition enforced
- Bedrock data governance reviewed; prompt injection test suite executed
- Budget alerts set before starting cloud resources
