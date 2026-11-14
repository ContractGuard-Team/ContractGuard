using ContractGuard.Application.Abstractions;
using ContractGuard.Domain.Common;
using ContractGuard.Domain.Contracts;
using Microsoft.EntityFrameworkCore;

namespace ContractGuard.Application.Contracts;

// F-AUD-001. Record() only adds the entry: it is saved by the caller's SaveChanges, together with the
// change it describes, so the log never claims something that was rolled back.
public sealed class AuditLog(IApplicationDbContext db, ICurrentWorkspace workspace, IUserDirectory users, TimeProvider clock)
{
    private static readonly TimeSpan ViewDedupWindow = TimeSpan.FromHours(1);

    public void Record(Guid contractId, AuditAction action, string? detail = null) =>
        db.AuditEvents.Add(AuditEvent.Create(contractId, workspace.UserId, action, detail, clock.GetUtcNow()));

    // FR-AUD-001 "xem". shortcut: at most one "viewed" per user and contract per hour, so reloads and
    // tab switches do not flood the log; make the window configurable if auditors ask for every open.
    public async Task RecordViewAsync(Guid contractId, CancellationToken cancellationToken)
    {
        var since = clock.GetUtcNow() - ViewDedupWindow;
        var seen = await db.AuditEvents.AnyAsync(
            e => e.ContractId == contractId && e.UserAccountId == workspace.UserId && e.Action == AuditAction.Viewed && e.OccurredAt > since,
            cancellationToken);
        if (!seen)
        {
            Record(contractId, AuditAction.Viewed);
            await db.SaveChangesAsync(cancellationToken);
        }
    }

    // FR-AUD-002: the contract owner and organization owners only.
    public async Task<List<AuditEntry>> ListAsync(Guid contractId, CancellationToken cancellationToken)
    {
        var contract = await db.Contracts.AsNoTracking().SingleOrDefaultAsync(c => c.ContractId == contractId, cancellationToken)
            ?? throw new DomainException(ErrorCodes.NotFound, "Contract not found.");
        ContractAccess.EnsureOwner(contract, workspace);

        // shortcut: newest 500 entries, add paging when a contract's log gets longer than that.
        var events = await db.AuditEvents
            .Where(e => e.ContractId == contractId)
            .OrderByDescending(e => e.OccurredAt)
            .Take(500)
            .ToListAsync(cancellationToken);
        var names = await users.GetAsync(events.Select(e => e.UserAccountId), cancellationToken);
        return [.. events.Select(e => new AuditEntry(
            e.AuditEventId,
            e.Action,
            e.Detail,
            names.TryGetValue(e.UserAccountId, out var user) ? user.DisplayName : null,
            user?.Email,
            e.OccurredAt))];
    }
}
