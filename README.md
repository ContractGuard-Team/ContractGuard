# ContractGuard

B2B contract risk analysis project. Shared source repository for 3 AI members, Dev and Security.

## Ownership

| Directory | Owner |
|---|---|
| `contracts/` | Shared contracts (AI + Dev + Security) |
| `worker/` | AI team |
| `backend/`, `frontend/` | Dev team |
| `security/` | Security owner |
| `.github/workflows/` | Shared CI checks |

## Data path

Backend metadata -> queued worker job -> AI extraction (untrusted) -> schema / evidence
validation -> backend rules -> database -> frontend.

See `docs/architecture/data-contract.md` and `CONTRIBUTING.md`.

## Local schema check

```powershell
python -m pip install 'jsonschema>=4,<5'
python scripts/check_schemas.py
```

**Do not commit** real contracts, credentials, virtual environments or local output files.
