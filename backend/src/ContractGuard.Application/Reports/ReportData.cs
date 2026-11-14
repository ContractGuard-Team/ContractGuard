using ContractGuard.Application.Contracts;
using ContractGuard.Domain.Analysis;
using ContractGuard.Domain.Contracts;

namespace ContractGuard.Application.Reports;

// FR-EXP-002: everything a report contains, already filtered by scope and with the labels of the
// chosen language. Both renderers lay out the same data, so PDF and DOCX say the same (FR-EXP-003).
public sealed record ReportData(
    ReportText Text,
    string ContractName,
    int VersionNumber,
    string FileName,
    ApprovalStatus ApprovalStatus,
    ContextDetails? Context,
    ContractMetadata? Metadata,
    RiskScore? Score,
    string? OverallRecommendation,
    IReadOnlyDictionary<Severity, int> SeverityCounts,
    IReadOnlyList<FindingItem> Findings,
    IReadOnlyList<CheckResult> Checks,
    IReadOnlyDictionary<string, string> ClauseNames,
    AnalysisTrace Trace,
    ReportScope Scope,
    DateTimeOffset GeneratedAt)
{
    // "Điều 6. Limitation of Liability", or the worker id when the clause is unknown.
    public string ClauseName(string? clauseKey) =>
        clauseKey is null ? string.Empty : ClauseNames.TryGetValue(clauseKey, out var name) ? name : clauseKey;
}
