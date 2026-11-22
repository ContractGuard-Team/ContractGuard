using ContractGuard.Application.Abstractions;
using ContractGuard.Domain.Analysis;
using ContractGuard.Domain.Common;
using ContractGuard.Domain.Contracts;
using Microsoft.EntityFrameworkCore;

namespace ContractGuard.Application.Contracts;

// User edits on a contract the user can see. Owner and Member of a contract may do the first three
// (SRS §12.3), and the query filters already hide contracts the user has no role on. Rename, delete,
// restore are Owner only.
public sealed class ContractCommands(
    IApplicationDbContext db,
    ICurrentWorkspace workspace,
    ContractPurger purger,
    AuditLog audit,
    TimeProvider clock)
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
