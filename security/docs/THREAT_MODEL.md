# ContractGuard STRIDE v0.1

| ID | Boundary | Threat | Control | Validation |
|---|---|---|---|---|
| T01 | Browser→API | forged/expired Cognito token | JWT issuer/audience/signature/expiry validation | send malformed/expired token |
| T02 | API→S3 | leaked presigned upload URL | short TTL, unique opaque key, size/checksum strategy, quarantine | wrong key/expired URL rejected |
| T03 | S3 quarantine→parser | malicious PDF/DOCX | malware scan, file signature/size, isolated process, limits | malicious/safe fixtures |
| T04 | API→DB | cross-tenant BOLA | owner+tenant lookup, deny by default | tenant B IDs from tenant A |
| T05 | Worker→S3→C# | forged analysis.json | canonical job/tenant binding, immutable versions, schema, provenance | wrong tenant/job mismatch rejected |
| T06 | RAG→LLM | retrieved policy from wrong tenant | tenant-scoped retrieval BEFORE ranking/context | tenant A queries tenant B only data |
| T07 | contract→LLM | indirect prompt injection alters scoring | untrusted-data separation, structured extraction, rule scoring | malicious contract has no effect on score |
| T08 | SQS result consumer | duplicate or out-of-order job | idempotency & state machine; DLQ | replay message twice |
| T09 | Admin→policy | unauthorized rule edit | RBAC, review, approvals, immutable version/log | analyst cannot edit policy |
| T10 | Container→AWS | overly broad IAM | specific task roles, no static AWS keys | Access Analyzer, simulate permissions |
| T11 | logs | sensitive contract content leakage | scrub secrets/PII, metadata-only logs | inspect CloudWatch logs |
| T12 | export→Browser | unscoped reports/download URL | tenant-bound permission, short-lived access | cross-tenant export denied |

Tool: OWASP Threat Dragon; deliver diagram with boundaries and link each scenario to a security test. Production signoff requires threat model review with SE+AI.
