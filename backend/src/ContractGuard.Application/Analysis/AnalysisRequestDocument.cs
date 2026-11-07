using ContractGuard.Domain.Contracts;
using ContractGuard.Domain.Users;

namespace ContractGuard.Application.Analysis;

// C# mirror of contracts/analysis-request.v1.schema.json: the job details the backend writes to
// results/{job_id}/request.json for the worker. Its Context is ContextDetails, which already has the
// fields of the schema's "context" object.
public sealed record AnalysisRequestDocument(
    string SchemaVersion,
    Guid JobId,
    string TenantId,
    Guid ContractId,
    string ContractVersion,
    string SourceFileKey,
    string CleanFileKey,
    string ResultKey,
    FileType DeclaredFileType,
    UiLanguage OutputLanguage,
    ContextDetails Context,
    AnalysisRequestLimits Limits);

public sealed record AnalysisRequestLimits(int MaxPages, long MaxBytes);
