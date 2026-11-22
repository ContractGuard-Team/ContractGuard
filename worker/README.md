# AI worker (Python)

The worker lives in the AI team's own repo (`contract-risk-management`), not in this folder. It runs
in Docker on EC2 in the AI team's AWS account.

What the worker reads and writes, queues, S3 keys and IAM: `docs/07-worker-aws-integration.md`.
Shared schemas: `../contracts/` (`extraction.v2`, `analysis-request.v1`, `chat-request.v1`,
`queue-messages.v1`).

Do not trust or generate job/tenant/contract identifiers. Do not write business records to the database.
