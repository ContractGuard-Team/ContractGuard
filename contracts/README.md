# Shared data contracts

- `extraction.schema.json`: payload produced by the AI worker (untrusted content).
- `job-result.schema.json`: envelope produced by the backend with **trusted** job/tenant/contract metadata.
- `extraction.v2.schema.json` (DRAFT): v2 result written by the worker to `results/{job_id}/extraction.json` (adds clauses, findings, checks, metadata, injection warnings, embeddings; SD-10).
- `analysis-request.v1.schema.json` (DRAFT): job details written by the backend to `results/{job_id}/request.json`.
- `queue-messages.v1.schema.json` (DRAFT): SQS message bodies on `analysis-requests` and `analysis-results` (SD-11).
- `examples/`: sanitized examples only. Never commit real contracts or customer data.
  `sample-contract.pdf` is synthetic and matches `extraction.v2.example.json` (text and regions); it is
  built by `scripts/dev/fixtures/make_sample_contract.py` and used by the dev seed and e2e tests.

`extraction.v2` has a `scoring` block proposed by SE on 2026-11-10 (optional until agreed). Open
questions and what the backend checks: `docs/06-ai-integration-points.md`.

Schema changes require an agreed version bump and review from AI + Backend + Security.
