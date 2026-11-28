using ContractGuard.Application.Abstractions;
using ContractGuard.Application.Analysis;
using ContractGuard.Domain.Analysis;
using ContractGuard.Domain.Common;
using ContractGuard.Domain.Contracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ContractGuard.Application.Contracts;

// User edits on a contract the user can see. Owner and Member of a contract may do the first three
// (SRS §12.3), and the query filters already hide contracts the user has no role on. Rename, delete,
// restore are Owner only.
public sealed partial class ContractCommands(
    IApplicationDbContext db,
    ICurrentWorkspace workspace,
    ContractPurger purger,
    AuditLog audit,
    IAnalysisJobQueue queue,
    TimeProvider clock,
    ILogger<ContractCommands> logger)
{
    // FR-CTR-007
    public async Task EditMetadataAsync(Guid contractId, Guid contractVersionId, ContractMetadata metadata, CancellationToken cancellationToken)
    {
        var version = await db.ContractVersions
            .SingleOrDefaultAsync(v => v.ContractId == contractId && v.ContractVersionId == contractVersionId && v.DeletedAt == null, cancellationToken)
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

    // FR-ANA-018 (CR-ANA-001): Owner and Member, like starting an analysis. A run that ended before the click is
    // left as it is; the page then shows its real outcome.
    public async Task CancelAnalysisAsync(Guid jobId, CancellationToken cancellationToken)
    {
        var run = await db.AnalysisRuns.SingleOrDefaultAsync(r => r.AnalysisRunId == jobId, cancellationToken)
            ?? throw new DomainException(ErrorCodes.NotFound, "Job not found.");
        var now = clock.GetUtcNow();
        if (!run.Cancel(now))
        {
            return;
        }

        var contract = await db.Contracts.SingleAsync(c => c.ContractId == run.ContractId, cancellationToken);
        contract.MarkAnalysisFailed(now); // TR-CTR-003: back to the status before the analysis, as after a failure.
        var versionNumber = await db.ContractVersions
            .Where(v => v.ContractVersionId == run.ContractVersionId)
            .Select(v => v.VersionNumber)
            .SingleAsync(cancellationToken);
        audit.Record(run.ContractId, AuditAction.AnalysisCancelled, $"V{versionNumber}");
        await db.SaveChangesAsync(cancellationToken);

        // Best effort: the run is already cancelled here, and whatever the worker sends later is ignored.
        // Without the message the worker only finishes the job for nothing (docs/07 §15). Not tied to the request's
        // token: the cancel is saved, so the worker is told even if the user has closed the page.
        try
        {
            await queue.PublishAsync(new AnalysisRequestMessage(AnalysisEventTypes.CancelRequested, run.AnalysisRunId, null), CancellationToken.None);
        }
        catch (Exception ex)
        {
            LogCancelNotSent(ex, run.AnalysisRunId);
        }
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Job {JobId} cancelled, but the worker could not be told")]
    private partial void LogCancelNotSent(Exception ex, Guid jobId);

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

    // CR-CTR-001: Owner only, allowed while an analysis runs (the name is not sent to the worker).
    public async Task RenameAsync(Guid contractId, string name, CancellationToken cancellationToken)
    {
        var contract = await db.Contracts.SingleOrDefaultAsync(c => c.ContractId == contractId, cancellationToken)
            ?? throw new DomainException(ErrorCodes.NotFound, "Contract not found.");
        ContractAccess.EnsureOwner(contract, workspace);
        var from = contract.ContractName;
        contract.Rename(name, clock.GetUtcNow());
        if (contract.ContractName != from)
        {
            audit.Record(contractId, AuditAction.Renamed, $"{from} -> {contract.ContractName}");
            await db.SaveChangesAsync(cancellationToken);
        }
    }

    // F-CTR-006 with CR-CTR-001: deleting moves the contract to the trash. It disappears from every view
    // at once (query filter) and is purged after Contract.TrashRetention, or earlier from the trash.
    public async Task MoveToTrashAsync(Guid contractId, CancellationToken cancellationToken)
    {
        var contract = await db.Contracts.SingleOrDefaultAsync(c => c.ContractId == contractId, cancellationToken)
            ?? throw new DomainException(ErrorCodes.NotFound, "Contract not found.");
        ContractAccess.EnsureOwner(contract, workspace); // FR-CTR-019
        contract.MoveToTrash(workspace.UserId, clock.GetUtcNow()); // ERR-CTR-003 while analyzing
        audit.Record(contractId, AuditAction.Trashed);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task RestoreAsync(Guid contractId, CancellationToken cancellationToken)
    {
        var contract = await FindInTrashAsync(contractId, cancellationToken);
        contract.RestoreFromTrash();
        audit.Record(contractId, AuditAction.Restored);
        await db.SaveChangesAsync(cancellationToken);
    }

    // Soft delete of one version, Owner only like deleting the contract. ERR-CTR-003 while analyzing.
    public async Task DeleteVersionAsync(Guid contractId, Guid contractVersionId, CancellationToken cancellationToken)
    {
        var contract = await LoadOwnedWithVersionsAsync(contractId, cancellationToken);
        contract.DeleteVersion(contractVersionId, workspace.UserId, clock.GetUtcNow());
        audit.Record(contractId, AuditAction.VersionDeleted, $"V{contract.GetVersion(contractVersionId).VersionNumber}");
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task RestoreVersionAsync(Guid contractId, Guid contractVersionId, CancellationToken cancellationToken)
    {
        var contract = await LoadOwnedWithVersionsAsync(contractId, cancellationToken);
        contract.RestoreVersion(contractVersionId, clock.GetUtcNow());
        audit.Record(contractId, AuditAction.VersionRestored, $"V{contract.GetVersion(contractVersionId).VersionNumber}");
        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task<Contract> LoadOwnedWithVersionsAsync(Guid contractId, CancellationToken cancellationToken)
    {
        var contract = await db.Contracts.Include(c => c.Versions).SingleOrDefaultAsync(c => c.ContractId == contractId, cancellationToken)
            ?? throw new DomainException(ErrorCodes.NotFound, "Contract not found.");
        ContractAccess.EnsureOwner(contract, workspace);
        return contract;
    }

    // FR-CTR-018: only from the trash, so one click in the library can never destroy a contract.
    public async Task DeletePermanentlyAsync(Guid contractId, CancellationToken cancellationToken)
    {
        var contract = await FindInTrashAsync(contractId, cancellationToken);
        await purger.PurgeAsync(contract, cancellationToken);
    }

    // The workspace filter still applies: a contract of another workspace looks like it does not exist.
    private async Task<Contract> FindInTrashAsync(Guid contractId, CancellationToken cancellationToken)
    {
        var contract = await db.Contracts.IgnoreQueryFilters([QueryFilters.NotDeleted])
            .SingleOrDefaultAsync(c => c.ContractId == contractId && c.DeletedAt != null, cancellationToken)
            ?? throw new DomainException(ErrorCodes.NotFound, "Contract not found in the trash.");
        ContractAccess.EnsureOwner(contract, workspace);
        return contract;
    }
}
