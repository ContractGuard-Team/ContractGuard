# Backend (.NET / C#)

Implement API, authentication/authorization, tenant boundaries, job metadata, and rule orchestration.
The backend is the **only service writing the database**. Use `../contracts/` for shared schemas.
Derive tenant_id, contract_id, contract_version and job_id from the trusted backend context,
not from a model-produced payload or an unverified message field.

## Layout

Clean Architecture: `Domain` <- `Application` <- `Infrastructure` <- `Api` (checked by
`tests/ContractGuard.UnitTests/Architecture`).

## Run locally

```powershell
docker compose up -d                                   # from the repo root: Postgres, LocalStack, Mailpit
cd backend
dotnet tool restore
dotnet ef database update --project src/ContractGuard.Infrastructure --startup-project src/ContractGuard.Api
dotnet run --project src/ContractGuard.Api             # http://localhost:5080/health/ready
dotnet test                                            # unit + integration (needs Docker)
```

Until the AI worker exists, `scripts/dev/send-analysis-result.ps1` plays its part against LocalStack.
The whole stack in containers: `docker compose --profile app up -d --build` (API on port 8080).
