using ContractGuard.Application.Abstractions;
using ContractGuard.Application.Analysis;
using ContractGuard.Domain.Analysis;
using ContractGuard.Domain.Common;
using ContractGuard.Domain.Contracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ContractGuard.Application.Contracts;

// User edits on a contract the user can see. Owner and Member of a contract may do the first three
// (SRS §12.3), and the query filters already hide contracts the user has no role on. Delete is Owner only.
public sealed partial class ContractCommands(
    IApplicationDbContext db,
    ICurrentWorkspace workspace,
    IObjectStorage storage,
    AuditLog audit,
    TimeProvider clock,
    ILogger<ContractCommands> logger)
{
    // FR-CTR-007
    public async Task EditMetadataAsync(Guid contractId, Guid contractVersionId, ContractMetadata metadata, CancellationToken cancellationToken)
    {
        var version = await db.ContractVersions
            .SingleOrDefaultAsync(v => v.ContractId == contractId && v.ContractVersionId == contractVersionId, cancellationToken)
            ?? throw new DomainException(ErrorCodes.NotFound, "Contract version not found.");
        version.EditMetadata(metadata, workspace.UserId, clock.GetUtcNow());
        audit.Record(contractId, AuditAction.MetadataEdited, $"V{version.VersionNumber}");
        await db.SaveChangesAsync(cancellationToken);
    }

    // FR-CTR-012, FR-CTR-013
    public async Task ChangeApprovalStatusAsync(Guid contractId, ApprovalStatus target, CancellationToken cancellationToken)
    {
        var contract = await db.Contracts.SingleOrDefaultAsync(c => c.ContractId == contractId, cancellationToken)
            ?? throw new DomainException(ErrorCodes.NotFound, "Contract not found.");
        var from = contract.ApprovalStatus;
        contract.ChangeApprovalStatus(target, clock.GetUtcNow());
        audit.Record(contractId, AuditAction.StatusChanged, $"{from} -> {target}");
        await db.SaveChangesAsync(cancellationToken);
    }

    // FR-VIEW-013, FR-VIEW-014
    public async Task SetFindingHandlingAsync(Guid findingId, HandlingStatus status, string? note, CancellationToken cancellationToken)
    {
        var finding = await db.Findings.SingleOrDefaultAsync(f => f.FindingId == findingId, cancellationToken)
            ?? throw new DomainException(ErrorCodes.NotFound, "Finding not found.");
        if (note?.Trim().Length > Finding.NoteMaxLength)
        {
            throw new DomainException(ErrorCodes.Validation, $"The note is at most {Finding.NoteMaxLength} characters.");
        }

        finding.SetHandling(status, note, workspace.UserId, clock.GetUtcNow());
        var contractId = await db.AnalysisRuns.Where(r => r.AnalysisRunId == finding.AnalysisRunId).Select(r => r.ContractId).SingleAsync(cancellationToken);
        audit.Record(contractId, AuditAction.FindingHandled, $"{finding.FindingKey}: {status}");
        await db.SaveChangesAsync(cancellationToken);
    }

    // F-CTR-006 (FR-CTR-018, FR-CTR-019): files first, then every row. Storage goes first so that a
    // failure leaves at worst a row pointing at a missing file (the viewer says so), never a contract
    // file nobody can find to delete. Rows cascade: versions, contexts, runs, clauses, findings, chat,
    // shares, log. Not while an analysis runs: the worker would write results for a deleted job.
    public async Task DeleteAsync(Guid contractId, CancellationToken cancellationToken)
    {
        var contract = await db.Contracts.Include(c => c.Versions).SingleOrDefaultAsync(c => c.ContractId == contractId, cancellationToken)
            ?? throw new DomainException(ErrorCodes.NotFound, "Contract not found.");
        ContractAccess.EnsureOwner(contract, workspace);
        if (contract.ApprovalStatus == ApprovalStatus.Analyzing)
        {
            throw new DomainException(ErrorCodes.ContractBusy, "The contract is being analyzed.");
        }

        var runIds = await db.AnalysisRuns.Where(r => r.ContractId == contractId).Select(r => r.AnalysisRunId).ToListAsync(cancellationToken);

        // AI-HOOKUP: the worker moves a file from quarantine/ to clean/ (SD-07); only the key the version
        // points at is known here, so the worker must delete the quarantine copy itself (docs/06).
        // The clean key is predictable, so a clean copy of a failed run (never recorded on the version) goes too.
        var workspaceSegment = AnalysisStorageKeys.WorkspaceSegment(contract.OrganizationId, contract.OwnerUserAccountId);
        string[] keys =
        [
            .. contract.Versions.Select(v => v.SourceFileKey),
            .. contract.Versions.Select(v => AnalysisStorageKeys.CleanKey(workspaceSegment, contractId, v.VersionNumber, Path.GetExtension(v.OriginalFileName).ToLowerInvariant())),
            .. runIds.SelectMany(id => new[] { AnalysisStorageKeys.RequestKey(id), AnalysisStorageKeys.ExtractionKey(id) })
        ];
        foreach (var key in keys)
        {
            await storage.DeleteAsync(key, cancellationToken);
        }

        db.Contracts.Remove(contract);
        await db.SaveChangesAsync(cancellationToken);
        LogDeleted(contractId, keys.Length);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Contract {ContractId} deleted with {FileCount} stored files")]
    private partial void LogDeleted(Guid contractId, int fileCount);
}
