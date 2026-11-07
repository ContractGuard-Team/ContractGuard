using ContractGuard.Domain.Analysis;
using ContractGuard.Domain.Contracts;

namespace ContractGuard.Application.Contracts;

// What ContractQueries returns to the API, kept together because they are read as one tree.
public sealed record ContractSummary(
    Guid ContractId,
    string ContractName,
    ApprovalStatus ApprovalStatus,
    int LatestVersionNumber,
    DateTimeOffset UpdatedAt);

public sealed record ContractDetail(
    Guid ContractId,
    string ContractName,
    ApprovalStatus ApprovalStatus,
    ContractVersionDetail LatestVersion,

    // The newest run of the latest version, whatever its status. Null until context is confirmed.
    JobStatus? LatestJob,

    // The run whose clauses to show. Can differ from LatestJob: a failed re-run keeps the old result (FR-ANA-015).
    Guid? CompletedJobId,

    // False while no AI worker is deployed (AnalysisOptions).
    bool AnalysisAvailable);

public sealed record ContractFileLink(string Url);

public sealed record ContractVersionDetail(
    Guid ContractVersionId,
    int VersionNumber,
    string OriginalFileName,
    FileType FileType,
    long FileSizeBytes,
    int? PageCount,
    ContextDetails? Context);

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
