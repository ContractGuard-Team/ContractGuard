using System.Security.Claims;
using ContractGuard.Application.Analysis;
using ContractGuard.Application.Contracts;
using ContractGuard.Application.Reports;
using ContractGuard.Domain.Users;
using ContractGuard.Domain.Analysis;
using ContractGuard.Domain.Common;
using ContractGuard.Domain.Contracts;
using ContractGuard.Infrastructure.Identity;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;

namespace ContractGuard.Api.Endpoints;

// F-CTR-001, F-ANA-001: upload a contract, confirm its context, follow the analysis, read the clauses.
internal static class ContractEndpoints
{
    public static IEndpointRouteBuilder MapContractEndpoints(this IEndpointRouteBuilder secured)
    {
        secured.MapPost("/uploads", RequestUpload).WithTags("Contracts");

        var contracts = secured.MapGroup("/contracts").WithTags("Contracts");
        contracts.MapGet("/", ListContracts);
        contracts.MapPost("/", CreateContract);
        contracts.MapGet("/{contractId:guid}", GetContract);
        contracts.MapPut("/{contractId:guid}/versions/{versionId:guid}/context", ConfirmContext);
        contracts.MapGet("/{contractId:guid}/versions/{versionId:guid}/file", GetFileLink);
        contracts.MapPut("/{contractId:guid}/versions/{versionId:guid}/metadata", EditMetadata);
        contracts.MapPut("/{contractId:guid}/approval-status", ChangeApprovalStatus);
        contracts.MapPost("/{contractId:guid}/versions", AddVersion);
        contracts.MapDelete("/{contractId:guid}", DeleteContract);
        contracts.MapGet("/{contractId:guid}/audit", GetAudit);
        contracts.MapGet("/{contractId:guid}/shares", GetShares);
        contracts.MapPost("/{contractId:guid}/shares", Share);
        contracts.MapDelete("/{contractId:guid}/shares/{userId:guid}", RevokeShare);
        contracts.MapPut("/{contractId:guid}/owner", TransferOwner);

        var jobs = secured.MapGroup("/jobs").WithTags("Jobs");
        jobs.MapGet("/{jobId:guid}", GetJob);
        jobs.MapGet("/{jobId:guid}/clauses", GetClauses);
        jobs.MapGet("/{jobId:guid}/result", GetResult);
        jobs.MapGet("/{jobId:guid}/report", ExportReport);

        secured.MapPut("/findings/{findingId:guid}/handling", SetFindingHandling).WithTags("Findings");
        return secured;
    }

    private static async Task<Ok<UploadTicket>> RequestUpload(UploadRequest request, ContractUploadService uploads, CancellationToken cancellationToken) =>
        TypedResults.Ok(await uploads.RequestUploadAsync(request.FileName, request.SizeBytes, cancellationToken));

    private static async Task<Ok<CreatedContract>> CreateContract(CreateContractRequest request, ContractUploadService uploads, CancellationToken cancellationToken) =>
        TypedResults.Ok(await uploads.CreateContractAsync(request.UploadId, request.FileName, cancellationToken));

    // F-CTR-003. Query values are matched by name like the report options (QueryEnum below).
    private static async Task<Ok<LibraryPage>> ListContracts(
        ContractQueries queries,
        CancellationToken cancellationToken,
        string? search = null,
        string? riskLevel = null,
        string? status = null,
        string? contractType = null,
        string? sort = null,
        bool descending = true,
        int page = 1,
        int pageSize = 20) =>
        TypedResults.Ok(await queries.ListAsync(
            new LibraryQuery(
                search,
                riskLevel is null ? null : QueryEnum(riskLevel, RiskLevel.Low),
                status is null ? null : QueryEnum(status, ApprovalStatus.New),
                contractType is null ? null : QueryEnum(contractType, ContractType.Other),
                QueryEnum(sort, LibrarySort.Updated),
                descending,
                page,
                pageSize),
            cancellationToken));

    // FR-AUD-001 "xem": opening a contract is logged (at most hourly per person).
    private static async Task<Ok<ContractDetail>> GetContract(Guid contractId, ContractQueries queries, AuditLog audit, CancellationToken cancellationToken, Guid? version = null)
    {
        var detail = await queries.GetAsync(contractId, version, cancellationToken);
        await audit.RecordViewAsync(contractId, cancellationToken);
        return TypedResults.Ok(detail);
    }

    private static async Task<Ok<CreatedContract>> AddVersion(Guid contractId, CreateContractRequest request, ContractUploadService uploads, CancellationToken cancellationToken) =>
        TypedResults.Ok(await uploads.AddVersionAsync(contractId, request.UploadId, request.FileName, cancellationToken));

    private static async Task<NoContent> DeleteContract(Guid contractId, ContractCommands commands, CancellationToken cancellationToken)
    {
        await commands.DeleteAsync(contractId, cancellationToken);
        return TypedResults.NoContent();
    }

    private static async Task<Ok<List<AuditEntry>>> GetAudit(Guid contractId, AuditLog audit, CancellationToken cancellationToken) =>
        TypedResults.Ok(await audit.ListAsync(contractId, cancellationToken));

    private static async Task<Ok<ContractAccessView>> GetShares(Guid contractId, ShareService shares, CancellationToken cancellationToken) =>
        TypedResults.Ok(await shares.ListAsync(contractId, cancellationToken));

    // F-SHARE-001: one email, or the whole organization (FR-SHARE-005).
    private static async Task<NoContent> Share(Guid contractId, ShareRequest request, ShareService shares, CancellationToken cancellationToken)
    {
        if (request.WholeOrganization)
        {
            await shares.ShareWithOrganizationAsync(contractId, cancellationToken);
        }
        else
        {
            await shares.ShareWithEmailAsync(contractId, request.Email ?? string.Empty, cancellationToken);
        }

        return TypedResults.NoContent();
    }

    private static async Task<NoContent> RevokeShare(Guid contractId, Guid userId, ShareService shares, CancellationToken cancellationToken)
    {
        await shares.RevokeAsync(contractId, userId, cancellationToken);
        return TypedResults.NoContent();
    }

    private static async Task<NoContent> TransferOwner(Guid contractId, TransferOwnerRequest request, ShareService shares, CancellationToken cancellationToken)
    {
        await shares.TransferOwnershipAsync(contractId, request.UserId, cancellationToken);
        return TypedResults.NoContent();
    }

    private static async Task<Ok<ContractFileLink>> GetFileLink(Guid contractId, Guid versionId, ContractQueries queries, CancellationToken cancellationToken) =>
        TypedResults.Ok(await queries.GetFileLinkAsync(contractId, versionId, cancellationToken));

    private static async Task<Results<Accepted<JobStarted>, NoContent>> ConfirmContext(
        Guid contractId,
        Guid versionId,
        ContextRequest request,
        ClaimsPrincipal principal,
        UserManager<AppUser> users,
        AnalysisStarter starter,
        CancellationToken cancellationToken)
    {
        var user = await users.GetUserAsync(principal)
            ?? throw new DomainException(ErrorCodes.Unauthenticated, "The account no longer exists.");
        var jobId = await starter.ConfirmContextAndStartAsync(contractId, versionId, request.ToDetails(), user.UiLanguage, cancellationToken);
        if (jobId is null)
        {
            return TypedResults.NoContent();
        }

        return TypedResults.Accepted($"/api/v1/jobs/{jobId}", new JobStarted(jobId.Value));
    }

    private static async Task<Ok<JobStatus>> GetJob(Guid jobId, ContractQueries queries, CancellationToken cancellationToken) =>
        TypedResults.Ok(await queries.GetJobAsync(jobId, cancellationToken));

    private static async Task<Ok<List<ClauseItem>>> GetClauses(Guid jobId, ContractQueries queries, CancellationToken cancellationToken) =>
        TypedResults.Ok(await queries.GetClausesAsync(jobId, cancellationToken));

    private static async Task<Ok<AnalysisResultView>> GetResult(Guid jobId, ContractQueries queries, CancellationToken cancellationToken) =>
        TypedResults.Ok(await queries.GetResultAsync(jobId, cancellationToken));

    private static async Task<NoContent> EditMetadata(Guid contractId, Guid versionId, MetadataRequest request, ContractCommands commands, CancellationToken cancellationToken)
    {
        await commands.EditMetadataAsync(contractId, versionId, request.ToMetadata(), cancellationToken);
        return TypedResults.NoContent();
    }

    private static async Task<NoContent> ChangeApprovalStatus(Guid contractId, ApprovalStatusRequest request, ContractCommands commands, CancellationToken cancellationToken)
    {
        await commands.ChangeApprovalStatusAsync(contractId, request.Status, cancellationToken);
        return TypedResults.NoContent();
    }

    private static async Task<NoContent> SetFindingHandling(Guid findingId, FindingHandlingRequest request, ContractCommands commands, CancellationToken cancellationToken)
    {
        await commands.SetFindingHandlingAsync(findingId, request.Status, request.Note, cancellationToken);
        return TypedResults.NoContent();
    }

    // F-EXP-001, F-EXP-002. A GET, so the browser downloads it with a plain link and keeps the page.
    private static async Task<FileContentHttpResult> ExportReport(
        Guid jobId,
        ReportService reports,
        CancellationToken cancellationToken,
        string? format = null,
        string? language = null,
        string? scope = null)
    {
        var file = await reports.ExportAsync(
            jobId,
            QueryEnum(format, ReportFormat.Pdf),
            QueryEnum(language, UiLanguage.Vi),
            QueryEnum(scope, ReportScope.All),
            cancellationToken);
        return TypedResults.File(file.Content, file.ContentType, file.FileName);
    }

    // Minimal APIs match enum names case-sensitively and also accept numbers; the frontend sends "pdf".
    private static T QueryEnum<T>(string? value, T fallback)
        where T : struct, Enum
    {
        if (value is null)
        {
            return fallback;
        }

        var name = Enum.GetNames<T>().FirstOrDefault(n => n.Equals(value, StringComparison.OrdinalIgnoreCase))
            ?? throw new DomainException(ErrorCodes.Validation, $"'{value}' is not one of {string.Join(", ", Enum.GetNames<T>()).ToLowerInvariant()}.");
        return Enum.Parse<T>(name);
    }
}
