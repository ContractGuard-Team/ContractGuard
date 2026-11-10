namespace ContractGuard.Domain.Analysis;

// Score of one domain (ENT-DATA-013). A domain that does not apply has no score, only a reason (FR-VIEW-003).
// ReEnabled: N/A by context but turned back on because the contract has clauses of it (BR-RISK-004).
public sealed record DomainScore(RiskDomain Domain, bool Applicable, int? Score, RiskLevel? RiskLevel, string? NaReason, bool ReEnabled);
