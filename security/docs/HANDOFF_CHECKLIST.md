# ContractGuard integration handoff agreement

## ATTT hands off to AI owners
- [ ] `schemas/extraction.schema.json` (fact_key, value, status and evidence)
- [ ] 20 rule catalog in `rules/cyber_rules.yaml`
- [ ] `policies/demo_saas_policy.yaml` with fictional thresholds and context fields
- [ ] Contradictory clauses / missing appendix cases and prompt injection fixtures
- [ ] Source-evidence requirement: clause_id, page, quote must match canonical parsed document

## ATTT hands off to SE owner
- [ ] Versioned policy & rule YAML, written test semantics and expected statuses
- [ ] Exactly defined operator behavior, context applicability and not-found semantics
- [ ] Production C# rule evaluator design; never treat Python checker as the authoritative engine
- [ ] Threat model + role matrix + AWS IAM baseline for CDK implementation
- [ ] Audit provenance fields: source object version+hash, job/tenant/contract, model/prompt/rule/policy versions
- [ ] Security CI pipeline: COPY `.github/workflows/security-checks.yml` to **monorepo ROOT** `.github/workflows/`; GitHub will not load it from `security/.github/`

## Acceptance demo before merging
- [ ] AI extraction JSON passes shared JSON Schema
- [ ] C# reference conformance fixtures match Python output by rule/status
- [ ] All failed rules cite source evidence, else require review
- [ ] Tenant A cannot see contract, job, policy, vector retrieval or report of tenant B
- [ ] Scanner failure remains quarantined
- [ ] C# rejects forged/replayed/stale analysis result
- [ ] Prompt injection in synthetic contract does not override C# rules
- [ ] Cloud security controls peer reviewed and budget controls enabled

## Version & PR procedure
1. Modify rule YAML or policy in a `feature/security-...` branch.
2. Add/modify failing fixture test first; run all security tests.
3. Increment policy/rule set version and document reason for change.
4. Create PR with owner review from SE+AI; require passing CI and manual explanation of changed severity.
5. Merge to protected `main`; release tag e.g. `security-rules-v1.0.0` after integration tests.
6. For each final finding persist immutable versions so historical result can be reconstructed.
