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
        var workspace = AnalysisStorageKeys.WorkspaceSegment(contract.OrganizationId, contract.OwnerUserAccountId);
        var extension = Path.GetExtension(version.OriginalFileName).ToLowerInvariant();
        var cleanKey = AnalysisStorageKeys.CleanKey(workspace, contract.ContractId, version.VersionNumber, extension);
        var inputKey = await PrepareWorkerInputAsync(contract, version, cleanKey, extension, cancellationToken);
        var requestKey = AnalysisStorageKeys.RequestKey(run.AnalysisRunId);
        await storage.PutJsonAsync(requestKey, BuildRequest(contract, version, run, inputKey, cleanKey, outputLanguage), cancellationToken);
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

    // Returns the key the worker reads, which must exist under quarantine/ (the worker may read nothing else,
    // docs/07 §2). It is not always the version's own file, so that a failed run can always be run again:
    // - the worker deletes the quarantine file once it has sent `completed`, before this service has accepted
    //   the result, so a rejected result leaves the version pointing at a file that is gone;
    // - after an accepted result the version points at its checked copy under clean/.
    // In both cases a copy of the checked file is put under quarantine/ for this run.
    private async Task<string> PrepareWorkerInputAsync(Contract contract, ContractVersion version, string cleanKey, string extension, CancellationToken cancellationToken)
    {
        var inQuarantine = version.SourceFileKey.StartsWith("quarantine/", StringComparison.Ordinal);
        var sourceExists = await storage.GetSizeAsync(version.SourceFileKey, cancellationToken) is not null;
        if (inQuarantine && sourceExists)
        {
            return version.SourceFileKey;
        }

        var copyFrom = sourceExists ? version.SourceFileKey
            : await storage.GetSizeAsync(cleanKey, cancellationToken) is not null ? cleanKey
            : throw new DomainException(ErrorCodes.SourceFileMissing, "The contract file is no longer stored. Upload it again as a new version.");
        var inputKey = inQuarantine ? version.SourceFileKey : AnalysisStorageKeys.RetryInputKey(contract.OwnerUserAccountId, version.ContractVersionId, extension);

        await using var source = await storage.OpenReadAsync(copyFrom, cancellationToken);
        using var copy = new MemoryStream();
        await source.CopyToAsync(copy, cancellationToken);
        await storage.PutBytesAsync(inputKey, copy.ToArray(), Contracts.FileTypeDetector.ContentTypeOf(version.FileType), cancellationToken);
        LogSourceRestored(version.ContractVersionId, copyFrom, inputKey);
        return inputKey;
    }

    private static AnalysisRequestDocument BuildRequest(Contract contract, ContractVersion version, AnalysisRun run, string inputKey, string cleanKey, UiLanguage outputLanguage)
    {
        return new AnalysisRequestDocument(
            SchemaVersion: "1.0.0",
            run.AnalysisRunId,
            TenantId: contract.OrganizationId?.ToString() ?? $"personal:{contract.OwnerUserAccountId}",
            contract.ContractId,
            ContractVersion: version.VersionNumber.ToString(CultureInfo.InvariantCulture),
            SourceFileKey: inputKey,
            CleanFileKey: cleanKey,
            ResultKey: AnalysisStorageKeys.ExtractionKey(run.AnalysisRunId),
            version.FileType,
            outputLanguage,
            version.Context!.ToDetails(),
            new AnalysisRequestLimits(ContractFile.MaxPageCount, ContractFile.MaxSizeBytes));
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Analysis started as job {JobId}")]
    private partial void LogStarted(Guid jobId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Version {VersionId}: worker input copied from {From} to {To}")]
    private partial void LogSourceRestored(Guid versionId, string from, string to);

    [LoggerMessage(Level = LogLevel.Error, Message = "Job {JobId} could not be put on the queue")]
    private partial void LogPublishFailed(Exception ex, Guid jobId);
}
