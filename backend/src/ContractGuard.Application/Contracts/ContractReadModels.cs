using ContractGuard.Domain.Analysis;
using ContractGuard.Domain.Contracts;

namespace ContractGuard.Application.Contracts;

// What ContractQueries returns to the API, kept together because they are read as one tree.
public sealed record ContractSummary(
    Guid ContractId,
    string ContractName,
    ApprovalStatus ApprovalStatus,
    int LatestVersionNumber,
    DateTimeOffset UpdatedAt,
    string? CounterpartyName,
    ContractType? ContractType,
    int? OverallScore,
    RiskLevel? RiskLevel,

    // Someone else owns it: shared with the current user (FR-CTR-008).
    bool SharedWithMe);

public enum LibrarySort
{
    Updated,
    Name,
    Score,
    Status
}

public sealed record LibraryQuery(
    string? Search,
    RiskLevel? RiskLevel,
    ApprovalStatus? Status,
    ContractType? ContractType,
    LibrarySort Sort,
    bool Descending,
    int Page,
    int PageSize);

public sealed record LibraryPage(List<ContractSummary> Items, int Total, int Page, int PageSize);

// BR-SHARE-002 role of the current user on one contract.
public enum ContractRole
{
    Owner,
    Member
}

public sealed record VersionItem(
    Guid ContractVersionId,
    int VersionNumber,
    string OriginalFileName,
    DateTimeOffset UploadedAt,
    RunStatus? LatestRunStatus,
    int? OverallScore,
    RiskLevel? RiskLevel);

public sealed record ContractDetail(
    Guid ContractId,
    string ContractName,
    ApprovalStatus ApprovalStatus,

    // Null for a personal contract.
    Guid? OrganizationId,
    ContractRole MyRole,

    // The version shown: the latest unless another was asked for (FR-CTR-016).
    ContractVersionDetail Version,
    bool IsLatestVersion,

    // FR-CTR-015: context to prefill when the shown version has none yet.
    ContextDetails? SuggestedContext,
    List<VersionItem> Versions,

    // The newest run of the shown version, whatever its status. Null until context is confirmed.
    JobStatus? LatestJob,

    // The run whose results to show. Can differ from LatestJob: a failed re-run keeps the old result (FR-ANA-015).
    Guid? CompletedJobId,

    // False while no AI worker is deployed (AnalysisOptions).
    bool AnalysisAvailable);

public sealed record AuditEntry(Guid AuditEventId, AuditAction Action, string? Detail, string? ActorName, string? ActorEmail, DateTimeOffset OccurredAt);

public sealed record ContractAccessView(PersonItem Owner, bool IsOrganizationContract, List<ShareItem> Shares);

public sealed record PersonItem(Guid UserId, string DisplayName, string Email);

public sealed record ShareItem(Guid UserId, string DisplayName, string Email, DateTimeOffset SharedAt);

public sealed record ContractFileLink(string Url);

public sealed record ContractVersionDetail(
    Guid ContractVersionId,
    int VersionNumber,
    string OriginalFileName,
    FileType FileType,
    long FileSizeBytes,
    int? PageCount,
    ContextDetails? Context,

    // F-CTR-002: the effective value is EditedMetadata when present, else ExtractedMetadata.
    ContractMetadata? ExtractedMetadata,
    ContractMetadata? EditedMetadata);

public sealed record JobStatus(
    Guid JobId,
    Guid ContractId,
    Guid ContractVersionId,
    RunStatus Status,
    AnalysisStep Step,
    string? FailureReason,
    DateTimeOffset StartedAt,
    DateTimeOffset? FinishedAt);

public sealed record ClauseItem(
    Guid ClauseId,
    string ClauseKey,
    int ClauseOrder,
    string ClauseNumber,
    string? Heading,
    string ClauseText,
    string ClauseType,
    ClauseLocation Location);

public sealed record FindingItem(
    Guid FindingId,
    string FindingKey,
    FindingKind Kind,
    string Title,
    RiskDomain PrimaryDomain,
    List<RiskDomain> SecondaryDomains,
    string? Category,
    Severity Severity,
    string? ClauseKey,
    List<Evidence> Evidence,
    string? MissingDescription,
    string Why,
    string Impact,
    string Recommendation,
    List<FindingReference> References,
    string? CheckCode,
    HandlingStatus HandlingStatus,
    string? HandlingNote,
    DateTimeOffset? HandledAt);

// Everything of a completed run except the clauses (F-VIEW-001, F-RISK-002, F-SEC-001).
// Score is null while the worker does not send the scoring block (docs/06-ai-integration-points.md).
public sealed record AnalysisResultView(
    Guid JobId,
    AnalysisTrace Trace,
    RiskScore? Score,
    string? OverallRecommendation,
    List<FindingItem> Findings,
    List<CheckResult> Checks,
    List<InjectionWarning> InjectionWarnings);
