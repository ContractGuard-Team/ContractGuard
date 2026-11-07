using System.Security.Claims;
using ContractGuard.Application.Analysis;
using ContractGuard.Application.Contracts;
using ContractGuard.Domain.Common;
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

        var jobs = secured.MapGroup("/jobs").WithTags("Jobs");
        jobs.MapGet("/{jobId:guid}", GetJob);
        jobs.MapGet("/{jobId:guid}/clauses", GetClauses);
        return secured;
    }

    private static async Task<Ok<UploadTicket>> RequestUpload(UploadRequest request, ContractUploadService uploads, CancellationToken cancellationToken) =>
        TypedResults.Ok(await uploads.RequestUploadAsync(request.FileName, request.SizeBytes, cancellationToken));

    private static async Task<Ok<CreatedContract>> CreateContract(CreateContractRequest request, ContractUploadService uploads, CancellationToken cancellationToken) =>
        TypedResults.Ok(await uploads.CreateContractAsync(request.UploadId, request.FileName, cancellationToken));

    private static async Task<Ok<List<ContractSummary>>> ListContracts(ContractQueries queries, CancellationToken cancellationToken) =>
        TypedResults.Ok(await queries.ListAsync(cancellationToken));

    private static async Task<Ok<ContractDetail>> GetContract(Guid contractId, ContractQueries queries, CancellationToken cancellationToken) =>
        TypedResults.Ok(await queries.GetAsync(contractId, cancellationToken));

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
}
