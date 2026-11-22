using ContractGuard.Application.Abstractions;
using ContractGuard.Application.Analysis;
using ContractGuard.Domain.Contracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ContractGuard.Application.Contracts;

// FR-CTR-018 with CR-CTR-001: removes a contract for good, when the owner empties it from the trash or
// when it has been there longer than Contract.TrashRetention (TrashPurgeService, run as system).
public sealed partial class ContractPurger(IApplicationDbContext db, IObjectStorage storage, TimeProvider clock, ILogger<ContractPurger> logger)
{
    // shortcut: at most this many contracts per pass; the next pass (hourly) takes the rest.
    private const int BatchSize = 50;

    public async Task<int> PurgeExpiredAsync(CancellationToken cancellationToken)
    {
        var cutoff = clock.GetUtcNow() - Contract.TrashRetention;
        var expired = await db.Contracts.IgnoreQueryFilters([QueryFilters.NotDeleted])
            .Where(c => c.DeletedAt != null && c.DeletedAt <= cutoff)
            .OrderBy(c => c.DeletedAt)
            .Take(BatchSize)
            .ToListAsync(cancellationToken);
        foreach (var contract in expired)
        {
            await PurgeAsync(contract, cancellationToken);
        }

        return expired.Count;
    }

    // Files first, then every row. Storage goes first so that a failure leaves at worst a row pointing
    // at a missing file, never a contract file nobody can find to delete. Rows cascade in the database:
    // versions, contexts, runs, clauses, findings, chat, shares, log. A contract in the trash cannot be
    // analyzing (Contract.MoveToTrash), so no worker writes for it any more.
    public async Task PurgeAsync(Contract contract, CancellationToken cancellationToken)
    {
        var contractId = contract.ContractId;
        var versions = await db.ContractVersions.IgnoreQueryFilters([QueryFilters.NotDeleted])
            .Where(v => v.ContractId == contractId)
            .Select(v => new { v.ContractVersionId, v.SourceFileKey, v.VersionNumber, v.OriginalFileName })
            .ToListAsync(cancellationToken);
        var runIds = await db.AnalysisRuns.IgnoreQueryFilters([QueryFilters.NotDeleted])
            .Where(r => r.ContractId == contractId)
            .Select(r => r.AnalysisRunId)
            .ToListAsync(cancellationToken);

        // The worker deletes a quarantine file only after "completed"; the clean and retry-input keys are
        // predictable, so copies that were never recorded on the version go too.
        var workspaceSegment = AnalysisStorageKeys.WorkspaceSegment(contract.OrganizationId, contract.OwnerUserAccountId);
        string[] keys =
        [
            .. versions.Select(v => v.SourceFileKey),
            .. versions.Select(v => AnalysisStorageKeys.CleanKey(workspaceSegment, contractId, v.VersionNumber, Extension(v.OriginalFileName))),
            .. versions.Select(v => AnalysisStorageKeys.RetryInputKey(contract.OwnerUserAccountId, v.ContractVersionId, Extension(v.OriginalFileName))),
            .. runIds.SelectMany(id => new[] { AnalysisStorageKeys.RequestKey(id), AnalysisStorageKeys.ExtractionKey(id) })
        ];
        foreach (var key in keys.Distinct())
        {
            await storage.DeleteAsync(key, cancellationToken);
        }

        db.Contracts.Remove(contract);
        await db.SaveChangesAsync(cancellationToken);
        LogPurged(contractId, keys.Length);
    }

    private static string Extension(string fileName) => Path.GetExtension(fileName).ToLowerInvariant();

    [LoggerMessage(Level = LogLevel.Information, Message = "Contract {ContractId} deleted for good with {FileCount} stored files")]
    private partial void LogPurged(Guid contractId, int fileCount);
}
