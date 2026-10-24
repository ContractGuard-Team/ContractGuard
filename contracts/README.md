# Shared data contracts

- `extraction.schema.json`: payload produced by the AI worker (untrusted content).
- `job-result.schema.json`: envelope produced by the backend with **trusted** job/tenant/contract metadata.
- `examples/`: sanitized examples only. Never commit real contracts or customer data.

Schema changes require an agreed version bump and review from AI + Backend + Security.
