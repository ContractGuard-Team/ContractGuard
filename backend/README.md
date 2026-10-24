# Backend (.NET / C#)

Implement API, authentication/authorization, tenant boundaries, job metadata, and rule orchestration.
The backend is the **only service writing the database**. Use `../contracts/` for shared schemas.
Derive tenant_id, contract_id, contract_version and job_id from the trusted backend context,
not from a model-produced payload or an unverified message field.
