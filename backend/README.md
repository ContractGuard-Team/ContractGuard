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

The API lives under `/api/v1`; `/openapi/v1.json` describes it in Development. Emails go to Mailpit
(http://localhost:8025). Contract data is limited to the current workspace by the query filters of
`AppDbContext`; background jobs must opt out explicitly with `CurrentWorkspace.RunAsSystem()`.

Until the AI worker exists, `Analysis:WorkerEnabled` is false in Development: contracts and contexts are
saved and no analysis starts. To test the analysis flow, set it to true and let
`scripts/dev/send-analysis-result.ps1` play the worker's part against LocalStack. Its result is the example
in `contracts/examples`, not an analysis of the uploaded file.
The whole stack in containers: `docker compose --profile app up -d --build` (API on port 8080).
