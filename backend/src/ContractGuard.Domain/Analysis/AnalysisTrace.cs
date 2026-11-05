namespace ContractGuard.Domain.Analysis;

// Versions of everything that produced a result (FR-ANA-016, BR-DATA-002).
public sealed record AnalysisTrace(string SchemaVersion, string? ModelVersion, string? PromptVersion, string? RuleVersion);
