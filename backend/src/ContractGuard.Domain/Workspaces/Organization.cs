using ContractGuard.Domain.Common;

namespace ContractGuard.Domain.Workspaces;

public sealed class Organization
{
    public const int NameMaxLength = 200;

    private readonly List<Membership> _memberships = [];

    private Organization()
    {
    }

    public Guid OrganizationId { get; private set; }
    public string OrganizationName { get; private set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; private set; }
    public IReadOnlyCollection<Membership> Memberships => _memberships;

    public static Organization Create(string name, Guid ownerUserAccountId, DateTimeOffset now)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        name = name.Trim();
        ArgumentOutOfRangeException.ThrowIfGreaterThan(name.Length, NameMaxLength, nameof(name));

        var organization = new Organization
        {
            OrganizationId = Guid.CreateVersion7(),
            OrganizationName = name,
            CreatedAt = now
        };
        organization._memberships.Add(
            Membership.Create(organization.OrganizationId, ownerUserAccountId, OrgRole.Owner, now));
        return organization;
    }

    // FR-ORG-012
    public void Rename(string name)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Trim().Length > NameMaxLength)
        {
            throw new DomainException(ErrorCodes.Validation, $"An organization name has 1 to {NameMaxLength} characters.");
        }

        OrganizationName = name.Trim();
    }

    public OrgRole? RoleOf(Guid userAccountId) => _memberships.SingleOrDefault(m => m.UserAccountId == userAccountId)?.OrgRole;

    // FR-ORG-004: an accepted invitation makes the person a Member.
    public void AddMember(Guid userAccountId, DateTimeOffset now)
    {
        if (RoleOf(userAccountId) is not null)
        {
            throw new DomainException(ErrorCodes.Validation, "This account is already a member of the organization.");
        }

        _memberships.Add(Membership.Create(OrganizationId, userAccountId, OrgRole.Member, now));
    }

    // FR-ORG-007, FR-ORG-008, FR-ORG-010
    public void ChangeRole(Guid userAccountId, OrgRole role)
    {
        var membership = Find(userAccountId);
        if (membership.OrgRole == OrgRole.Owner && role != OrgRole.Owner)
        {
            EnsureAnotherOwner(userAccountId);
        }

        membership.ChangeRole(role);
    }

    // FR-ORG-009, FR-ORG-010. Moving the member's contracts (FR-ORG-011) is done by the caller.
    public void RemoveMember(Guid userAccountId)
    {
        var membership = Find(userAccountId);
        if (membership.OrgRole == OrgRole.Owner)
        {
            EnsureAnotherOwner(userAccountId);
        }

        _memberships.Remove(membership);
    }

    private Membership Find(Guid userAccountId) =>
        _memberships.SingleOrDefault(m => m.UserAccountId == userAccountId)
        ?? throw new DomainException(ErrorCodes.NotFound, "Member not found.");

    private void EnsureAnotherOwner(Guid userAccountId)
    {
        if (!_memberships.Any(m => m.OrgRole == OrgRole.Owner && m.UserAccountId != userAccountId))
        {
            throw new DomainException(ErrorCodes.LastOwner, "An organization must keep at least one owner.");
        }
    }
}
