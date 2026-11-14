using ContractGuard.Application.Abstractions;
using ContractGuard.Domain.Common;
using ContractGuard.Domain.Contracts;
using Microsoft.EntityFrameworkCore;

namespace ContractGuard.Application.Contracts;

// F-SHARE-001. Only the contract owner (or an organization owner) manages access (BR-SHARE-002).
// Organization contracts are shared within the organization only; personal contracts with any
// registered account (BR-SHARE-003).
public sealed class ShareService(IApplicationDbContext db, ICurrentWorkspace workspace, IUserDirectory users, AuditLog audit, TimeProvider clock)
{
    // FR-SHARE-006
    public async Task<ContractAccessView> ListAsync(Guid contractId, CancellationToken cancellationToken)
    {
        var contract = await LoadOwnedAsync(contractId, cancellationToken);
        var shares = await db.ContractShares.Where(s => s.ContractId == contractId).OrderBy(s => s.SharedAt).ToListAsync(cancellationToken);
        var people = await users.GetAsync(shares.Select(s => s.UserAccountId).Append(contract.OwnerUserAccountId), cancellationToken);
        var owner = people.GetValueOrDefault(contract.OwnerUserAccountId);
        return new ContractAccessView(
            new PersonItem(contract.OwnerUserAccountId, owner?.DisplayName ?? "", owner?.Email ?? ""),
            contract.OrganizationId is not null,
            [.. shares.Select(s => new ShareItem(s.UserAccountId, people.GetValueOrDefault(s.UserAccountId)?.DisplayName ?? "", people.GetValueOrDefault(s.UserAccountId)?.Email ?? "", s.SharedAt))]);
    }

    // FR-SHARE-003, 004, 009, 010
    public async Task ShareWithEmailAsync(Guid contractId, string email, CancellationToken cancellationToken)
    {
        var contract = await LoadOwnedAsync(contractId, cancellationToken);
        var user = await users.FindByEmailAsync((email ?? string.Empty).Trim(), cancellationToken);
        if (contract.OrganizationId is { } organizationId)
        {
            if (user is null || !await IsMemberAsync(organizationId, user.UserId, cancellationToken))
            {
                throw new DomainException(ErrorCodes.ShareNotOrgMember, "Organization contracts can only be shared with members of the organization.");
            }
        }
        else if (user is null)
        {
            throw new DomainException(ErrorCodes.ShareNoAccount, "No ContractGuard account uses this email.");
        }

        if (user.UserId == contract.OwnerUserAccountId)
        {
            throw new DomainException(ErrorCodes.Validation, "The owner already has access.");
        }

        if (await AddShareAsync(contract.ContractId, user.UserId, cancellationToken))
        {
            audit.Record(contract.ContractId, AuditAction.Shared, user.Email);
            await db.SaveChangesAsync(cancellationToken);
        }
    }

    // FR-SHARE-005: every member at the moment of sharing; later members are not added.
    public async Task ShareWithOrganizationAsync(Guid contractId, CancellationToken cancellationToken)
    {
        var contract = await LoadOwnedAsync(contractId, cancellationToken);
        var organizationId = contract.OrganizationId
            ?? throw new DomainException(ErrorCodes.Validation, "A personal contract has no organization to share with.");
        var members = await db.Memberships.Where(m => m.OrganizationId == organizationId).Select(m => m.UserAccountId).ToListAsync(cancellationToken);
        var added = 0;
        foreach (var member in members.Where(m => m != contract.OwnerUserAccountId))
        {
            added += await AddShareAsync(contract.ContractId, member, cancellationToken) ? 1 : 0;
        }

        audit.Record(contract.ContractId, AuditAction.Shared, $"whole organization ({added} new)");
        await db.SaveChangesAsync(cancellationToken);
    }

    // FR-SHARE-007, BR-SHARE-004: the row goes, the next request of that person no longer sees the contract.
    public async Task RevokeAsync(Guid contractId, Guid userAccountId, CancellationToken cancellationToken)
    {
        var contract = await LoadOwnedAsync(contractId, cancellationToken);
        var share = await db.ContractShares.SingleOrDefaultAsync(s => s.ContractId == contract.ContractId && s.UserAccountId == userAccountId, cancellationToken)
            ?? throw new DomainException(ErrorCodes.NotFound, "This person has no access to revoke.");
        db.ContractShares.Remove(share);
        var person = (await users.GetAsync([userAccountId], cancellationToken)).GetValueOrDefault(userAccountId);
        audit.Record(contract.ContractId, AuditAction.ShareRevoked, person?.Email);
        await db.SaveChangesAsync(cancellationToken);
    }

    // FR-SHARE-008: organization contracts only (a personal contract stays with its owner, §12.3).
    // The previous owner keeps working on it as a Member.
    public async Task TransferOwnershipAsync(Guid contractId, Guid newOwnerUserAccountId, CancellationToken cancellationToken)
    {
        var contract = await LoadOwnedAsync(contractId, cancellationToken);
        var organizationId = contract.OrganizationId
            ?? throw new DomainException(ErrorCodes.Validation, "A personal contract cannot change owner.");
        if (!await IsMemberAsync(organizationId, newOwnerUserAccountId, cancellationToken))
        {
            throw new DomainException(ErrorCodes.ShareNotOrgMember, "The new owner must be a member of the organization.");
        }

        var previousOwner = contract.OwnerUserAccountId;
        if (previousOwner == newOwnerUserAccountId)
        {
            return;
        }

        contract.TransferOwnership(newOwnerUserAccountId, clock.GetUtcNow());
        var oldShare = await db.ContractShares.SingleOrDefaultAsync(s => s.ContractId == contractId && s.UserAccountId == newOwnerUserAccountId, cancellationToken);
        if (oldShare is not null)
        {
            db.ContractShares.Remove(oldShare);
        }

        await AddShareAsync(contractId, previousOwner, cancellationToken);
        var person = (await users.GetAsync([newOwnerUserAccountId], cancellationToken)).GetValueOrDefault(newOwnerUserAccountId);
        audit.Record(contractId, AuditAction.OwnerTransferred, person?.Email);
        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task<Contract> LoadOwnedAsync(Guid contractId, CancellationToken cancellationToken)
    {
        var contract = await db.Contracts.SingleOrDefaultAsync(c => c.ContractId == contractId, cancellationToken)
            ?? throw new DomainException(ErrorCodes.NotFound, "Contract not found.");
        ContractAccess.EnsureOwner(contract, workspace);
        return contract;
    }

    private Task<bool> IsMemberAsync(Guid organizationId, Guid userAccountId, CancellationToken cancellationToken) =>
        db.Memberships.AnyAsync(m => m.OrganizationId == organizationId && m.UserAccountId == userAccountId, cancellationToken);

    private async Task<bool> AddShareAsync(Guid contractId, Guid userAccountId, CancellationToken cancellationToken)
    {
        if (await db.ContractShares.AnyAsync(s => s.ContractId == contractId && s.UserAccountId == userAccountId, cancellationToken)
            || db.ContractShares.Local.Any(s => s.ContractId == contractId && s.UserAccountId == userAccountId))
        {
            return false;
        }

        db.ContractShares.Add(ContractShare.Create(contractId, userAccountId, workspace.UserId, clock.GetUtcNow()));
        return true;
    }
}
