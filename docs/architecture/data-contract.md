# Data contract and trust boundary

1. Backend authenticates user, authorizes tenant, creates job and binds contract version.
2. Worker extracts `facts` and `evidence`, computes scores with versioned rules in Python (never from a model's number), then returns an **untrusted** extraction payload.
3. Backend validates payload against `contracts/extraction.v2.schema.json` (rules: `docs/06-ai-integration-points.md` §3.1).
4. Validator checks normalized fact keys/types, clause IDs, page numbers and evidence against trusted parsed text.
5. Backend attaches trusted job/tenant/contract identifiers and checks score limits and risk levels.
6. Persist validated results with audit logs and least-privilege storage.

`NOT_FOUND` means a completed search found no evidence; `UNKNOWN` means insufficient evidence.
Neither automatically implies legal or contractual noncompliance.
Do not store customer contracts or unredacted evidence in Git.
