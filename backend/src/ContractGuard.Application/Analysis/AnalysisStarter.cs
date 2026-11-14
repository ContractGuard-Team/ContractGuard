using System.Globalization;
using ContractGuard.Application.Abstractions;
using ContractGuard.Domain.Analysis;
using ContractGuard.Domain.Common;
using ContractGuard.Domain.Contracts;
using ContractGuard.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ContractGuard.Application.Analysis;

// FR-CTR-005 + FR-ANA-001: saving the context starts the analysis. Sending the current context again
// is also how a user re-runs an analysis (FR-ANA-014).
public sealed partial class AnalysisStarter(
    IApplicationDbContext db,
    IObjectStorage storage,
    IAnalysisJobQueue queue,
    Contracts.AuditLog audit,
    IOptions<AnalysisOptions> options,
    TimeProvider clock,
    ILogger<AnalysisStarter> logger)
{
    // The job id, or null when no worker is deployed and only the context was saved.
    public async Task<Guid?> ConfirmContextAndStartAsync(
        Guid contractId,
        Guid contractVersionId,
        ContextDetails details,
        UiLanguage outputLanguage,
        CancellationToken cancellationToken)
    {
        var contract = await db.Contracts
            .Include(c => c.Versions)
            .ThenInclude(v => v.Context)
            .SingleOrDefaultAsync(c => c.ContractId == contractId, cancellationToken)
            ?? throw new DomainException(ErrorCodes.NotFound, "Contract not found.");

        var now = clock.GetUtcNow();
        contract.ConfirmContext(contractVersionId, details, now); // ERR-CTR-003 if one is running.
        audit.Record(contract.ContractId, AuditAction.ContextConfirmed, $"V{contract.GetVersion(contractVersionId).VersionNumber}");
        if (!options.Value.WorkerEnabled)
        {
            await db.SaveChangesAsync(cancellationToken);
            return null;
        }

        contract.MarkAnalysisStarted(contractVersionId, now);
        var version = contract.GetVersion(contractVersionId);
        var run = AnalysisRun.Start(contract.ContractId, contractVersionId, now);
        db.AnalysisRuns.Add(run);

        using var scope = logger.BeginScope(new Dictionary<string, object> { ["job_id"] = run.AnalysisRunId });

        // Order matters. The file comes first: a leftover file is harmless. The database comes before
        // the queue: the worker must never receive a job the database does not know.
        var requestKey = AnalysisStorageKeys.RequestKey(run.AnalysisRunId);
        await storage.PutJsonAsync(requestKey, BuildRequest(contract, version, run, outputLanguage), cancellationToken);
        await SaveOrThrowBusyAsync(contract.ContractId, cancellationToken);

        try
        {
            await queue.PublishAsync(new AnalysisRequestMessage(AnalysisEventTypes.AnalysisRequested, run.AnalysisRunId, requestKey), cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Do not leave the user waiting 20 minutes for the sweeper. If the process dies right here,
            // the sweeper is what still ends the run.
            LogPublishFailed(ex, run.AnalysisRunId);
            run.Fail("The analysis could not be started.", now);
            contract.MarkAnalysisFailed(now);
            await db.SaveChangesAsync(CancellationToken.None);
            throw new DomainException(ErrorCodes.AnalysisFailed, "The analysis could not be started. Please try again.");
        }

        LogStarted(run.AnalysisRunId);
        return run.AnalysisRunId;
    }

    // Two requests can pass the status check at the same moment. The unique index on running
    // analyses (EDGE-ANA-003) then refuses the second one.
    private async Task SaveOrThrowBusyAsync(Guid contractId, CancellationToken cancellationToken)
    {
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            db.DiscardChanges();
            if (await db.AnalysisRuns.AnyAsync(r => r.ContractId == contractId && r.RunStatus == RunStatus.Running, cancellationToken))
            {
                throw new DomainException(ErrorCodes.ContractBusy, "The contract is being analyzed.");
            }

            throw;
        }
    }

    private static AnalysisRequestDocument BuildRequest(Contract contract, ContractVersion version, AnalysisRun run, UiLanguage outputLanguage)
    {
        var workspace = AnalysisStorageKeys.WorkspaceSegment(contract.OrganizationId, contract.OwnerUserAccountId);
        var extension = Path.GetExtension(version.OriginalFileName).ToLowerInvariant();
        return new AnalysisRequestDocument(
            SchemaVersion: "1.0.0",
            run.AnalysisRunId,
            TenantId: contract.OrganizationId?.ToString() ?? $"personal:{contract.OwnerUserAccountId}",
            contract.ContractId,
            ContractVersion: version.VersionNumber.ToString(CultureInfo.InvariantCulture),
            version.SourceFileKey,
            CleanFileKey: AnalysisStorageKeys.CleanKey(workspace, contract.ContractId, version.VersionNumber, extension),
            ResultKey: AnalysisStorageKeys.ExtractionKey(run.AnalysisRunId),
            version.FileType,
            outputLanguage,
            version.Context!.ToDetails(),
            new AnalysisRequestLimits(ContractFile.MaxPageCount, ContractFile.MaxSizeBytes));
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Analysis started as job {JobId}")]
    private partial void LogStarted(Guid jobId);

    [LoggerMessage(Level = LogLevel.Error, Message = "Job {JobId} could not be put on the queue")]
    private partial void LogPublishFailed(Exception ex, Guid jobId);
}
