using System.Net.Mail;
using ContractGuard.Application.Abstractions;
using ContractGuard.Domain.Common;
using ContractGuard.Domain.Contracts;
using ContractGuard.Domain.Workspaces;
using Microsoft.EntityFrameworkCore;

namespace ContractGuard.Application.Workspaces;

// F-ORG-001, F-ORG-002. Organization routes carry the organization id, so access is checked here from
// the membership table on every call (SEC-AUTHZ-008), not from the X-Workspace-Id header.
// A non-member gets 404: they do not learn that the organization exists.
public sealed class OrganizationService(IApplicationDbContext db, ICurrentWorkspace workspace, IUserDirectory users, TimeProvider clock)
{
    // FR-ORG-001
    public async Task<Guid> CreateAsync(string name, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Trim().Length > Organization.NameMaxLength)
        {
            throw new DomainException(ErrorCodes.Validation, $"An organization name has 1 to {Organization.NameMaxLength} characters.");
        }

        var organization = Organization.Create(name, workspace.UserId, clock.GetUtcNow());
        db.Organizations.Add(organization);
        await db.SaveChangesAsync(cancellationToken);
        return organization.OrganizationId;
    }

    // FR-ORG-012
    public async Task RenameAsync(Guid organizationId, string name, CancellationToken cancellationToken)
    {
        var organization = await LoadAsync(organizationId, requireOwner: true, cancellationToken);
        organization.Rename(name);
        await db.SaveChangesAsync(cancellationToken);
    }

    // FR-ORG-006: every member sees members with email and role; owners also see open invitations.
    public async Task<OrganizationDetail> GetAsync(Guid organizationId, CancellationToken cancellationToken)
    {
        var organization = await LoadAsync(organizationId, requireOwner: false, cancellationToken);
        var isOwner = organization.RoleOf(workspace.UserId) == OrgRole.Owner;
        var people = await users.GetAsync(organization.Memberships.Select(m => m.UserAccountId), cancellationToken);
        var members = organization.Memberships
            .OrderBy(m => m.JoinedAt)
            .Select(m => new MemberItem(m.UserAccountId, people.GetValueOrDefault(m.UserAccountId)?.DisplayName ?? "", people.GetValueOrDefault(m.UserAccountId)?.Email ?? "", m.OrgRole, m.JoinedAt))
            .ToList();
        var now = clock.GetUtcNow();
        var invitations = isOwner
            ? await db.Invitations
                .Where(i => i.OrganizationId == organizationId && i.ExpiresAt > now)
                .OrderBy(i => i.CreatedAt)
                .Select(i => new InvitationItem(i.InvitationId, i.Email, i.CreatedAt, i.ExpiresAt))
                .ToListAsync(cancellationToken)
            : [];
        return new OrganizationDetail(organization.OrganizationId, organization.OrganizationName, isOwner ? OrgRole.Owner : OrgRole.Member, members, invitations);
    }

    // FR-ORG-003. Returns the raw token for the email link; inviting the same email again replaces the old link.
    public async Task<CreatedInvitation> InviteAsync(Guid organizationId, string email, CancellationToken cancellationToken)
    {
        var organization = await LoadAsync(organizationId, requireOwner: true, cancellationToken);
        email = RequireEmail(email);

        var existing = await users.FindByEmailAsync(email, cancellationToken);
        if (existing is not null && organization.RoleOf(existing.UserId) is not null)
        {
            throw new DomainException(ErrorCodes.Validation, "This person is already a member of the organization.");
        }

        var old = await db.Invitations.Where(i => i.OrganizationId == organizationId && i.Email == email).ToListAsync(cancellationToken);
        db.Invitations.RemoveRange(old);
        if (old.Count > 0)
        {
            await db.SaveChangesAsync(cancellationToken); // the unique (organization, email) index would refuse the new row otherwise
        }

        var (invitation, token) = Invitation.Create(organizationId, email, workspace.UserId, clock.GetUtcNow());
        db.Invitations.Add(invitation);
        await db.SaveChangesAsync(cancellationToken);
        var inviter = (await users.GetAsync([workspace.UserId], cancellationToken)).GetValueOrDefault(workspace.UserId);
        return new CreatedInvitation(invitation.InvitationId, invitation.Email, organization.OrganizationName, inviter?.DisplayName ?? "", token);
    }

    public async Task CancelInvitationAsync(Guid organizationId, Guid invitationId, CancellationToken cancellationToken)
    {
        await LoadAsync(organizationId, requireOwner: true, cancellationToken);
        var invitation = await db.Invitations.SingleOrDefaultAsync(i => i.InvitationId == invitationId && i.OrganizationId == organizationId, cancellationToken)
            ?? throw new DomainException(ErrorCodes.NotFound, "Invitation not found.");
        db.Invitations.Remove(invitation);
        await db.SaveChangesAsync(cancellationToken);
    }

    // The page behind the email link, before the person decides (works without a session).
    public async Task<InvitationView> ReadInvitationAsync(string token, CancellationToken cancellationToken)
    {
        var invitation = await FindValidInvitationAsync(token, cancellationToken);
        var organization = await db.Organizations.AsNoTracking().SingleAsync(o => o.OrganizationId == invitation.OrganizationId, cancellationToken);
        var inviter = (await users.GetAsync([invitation.InvitedByUserAccountId], cancellationToken)).GetValueOrDefault(invitation.InvitedByUserAccountId);
        return new InvitationView(organization.OrganizationName, inviter?.DisplayName ?? "", invitation.Email, invitation.ExpiresAt);
    }

    // FR-ORG-004, FR-ORG-005. Only the invited address can accept: a forwarded link does not let someone else in.
    public async Task<Guid> AcceptAsync(string token, CancellationToken cancellationToken)
    {
        var invitation = await FindValidInvitationAsync(token, cancellationToken);
        var me = (await users.GetAsync([workspace.UserId], cancellationToken)).GetValueOrDefault(workspace.UserId);
        if (me is null || !string.Equals(me.Email, invitation.Email, StringComparison.OrdinalIgnoreCase))
        {
            throw new DomainException(ErrorCodes.Forbidden, "This invitation was sent to another email address.");
        }

        var organization = await db.Organizations.Include(o => o.Memberships).SingleAsync(o => o.OrganizationId == invitation.OrganizationId, cancellationToken);
        if (organization.RoleOf(workspace.UserId) is null)
        {
            organization.AddMember(workspace.UserId, clock.GetUtcNow());
        }

        db.Invitations.Remove(invitation); // TR-ORG-001
        await db.SaveChangesAsync(cancellationToken);
        return organization.OrganizationId;
    }

    // FR-ORG-007, FR-ORG-008, FR-ORG-010
    public async Task ChangeRoleAsync(Guid organizationId, Guid userAccountId, OrgRole role, CancellationToken cancellationToken)
    {
        var organization = await LoadAsync(organizationId, requireOwner: true, cancellationToken);
        organization.ChangeRole(userAccountId, role);
        await db.SaveChangesAsync(cancellationToken);
    }

    // FR-ORG-009, FR-ORG-010, FR-ORG-011: the removed member's contracts go to the owner who removed
    // them, and their shares on this organization's contracts end at once.
    public async Task RemoveMemberAsync(Guid organizationId, Guid userAccountId, CancellationToken cancellationToken)
    {
        if (userAccountId == workspace.UserId)
        {
            // Their contracts would have to go to themselves (FR-ORG-011); another owner removes them.
            throw new DomainException(ErrorCodes.Validation, "Owners cannot remove themselves; ask another owner.");
        }

        var organization = await LoadAsync(organizationId, requireOwner: true, cancellationToken);
        organization.RemoveMember(userAccountId);

        var now = clock.GetUtcNow();
        var owned = await db.Contracts.IgnoreQueryFilters()
            .Where(c => c.OrganizationId == organizationId && c.OwnerUserAccountId == userAccountId)
            .ToListAsync(cancellationToken);
        foreach (var contract in owned)
        {
            contract.TransferOwnership(workspace.UserId, now);
            db.AuditEvents.Add(AuditEvent.Create(contract.ContractId, workspace.UserId, AuditAction.OwnerTransferred, "member removed from organization", now));
        }

        var shares = await db.ContractShares
            .Where(s => s.UserAccountId == userAccountId && db.Contracts.IgnoreQueryFilters().Any(c => c.ContractId == s.ContractId && c.OrganizationId == organizationId))
            .ToListAsync(cancellationToken);
        db.ContractShares.RemoveRange(shares);
        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task<Organization> LoadAsync(Guid organizationId, bool requireOwner, CancellationToken cancellationToken)
    {
        var organization = await db.Organizations.Include(o => o.Memberships).SingleOrDefaultAsync(o => o.OrganizationId == organizationId, cancellationToken);
        var role = organization?.RoleOf(workspace.UserId);
        if (organization is null || role is null)
        {
            throw new DomainException(ErrorCodes.NotFound, "Organization not found.");
        }

        if (requireOwner && role != OrgRole.Owner)
        {
            throw new DomainException(ErrorCodes.Forbidden, "Only an owner of the organization can do this.");
        }

        return organization;
    }

    private async Task<Invitation> FindValidInvitationAsync(string token, CancellationToken cancellationToken)
    {
        var hash = Invitation.Hash(token ?? string.Empty);
        var invitation = await db.Invitations.SingleOrDefaultAsync(i => i.TokenHash == hash, cancellationToken);
        if (invitation is null || invitation.IsExpired(clock.GetUtcNow()))
        {
            if (invitation is not null)
            {
                db.Invitations.Remove(invitation); // TR-ORG-002: an expired invitation is deleted.
                await db.SaveChangesAsync(cancellationToken);
            }

            throw new DomainException(ErrorCodes.InvitationInvalid, "The invitation link is no longer valid.");
        }

        return invitation;
    }

    private static string RequireEmail(string email)
    {
        email = (email ?? string.Empty).Trim().ToLowerInvariant();
        if (email.Length is 0 or > 256 || !MailAddress.TryCreate(email, out var parsed) || parsed.Address != email)
        {
            throw new DomainException(ErrorCodes.Validation, "Enter a valid email address.");
        }

        return email;
    }
}
