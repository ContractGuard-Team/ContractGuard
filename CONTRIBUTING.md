# Working together

- `contracts/`: shared data schema; AI + Backend + Security must approve breaking changes.
- `security/`: Security owner. `worker/`: AI team. `backend/`, `frontend/`: Dev team.
- Use task-specific branches, open Pull Requests (PRs), and require review before merging to `main`.
- Run schema checks and tests before every PR.
- Never commit `.env`, `.venv`, tokens, keys, real contracts, private test reports or generated results.
- When a schema changes, update its version, consumer code, tests and documented sample together.
