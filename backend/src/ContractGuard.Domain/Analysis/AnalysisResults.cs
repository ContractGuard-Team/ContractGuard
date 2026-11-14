namespace ContractGuard.Domain.Analysis;

// Everything a completed run stores besides its clauses. Metadata goes to the version (REL-DATA-010).
public sealed record AnalysisResults(
    IEnumerable<FindingData> Findings,
    List<CheckResult> Checks,
    RiskScore? Score,
    List<InjectionWarning> InjectionWarnings,
    string? OverallRecommendation);
