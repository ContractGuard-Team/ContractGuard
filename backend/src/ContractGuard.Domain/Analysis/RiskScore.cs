namespace ContractGuard.Domain.Analysis;

// F-RISK-003: scores computed by the worker, checked by the backend (docs/05 §4).
public sealed record RiskScore(int OverallScore, RiskLevel RiskLevel, List<DomainScore> Domains);
