namespace ContractGuard.Domain.Workspaces;

public sealed class Membership
{
    private Membership() { }

    public Guid MembershipId { get; private set; }
    public Guid OrganizationId { get; private set; }
    public Guid UserAccountId { get; private set; }
    public OrgRole OrgRole { get; private set; }
    public DateTimeOffset JoinedAt { get; private set; }

    internal static Membership Create(Guid organizationId, Guid userAccountId, OrgRole role, DateTimeOffset now)
    {
        return new Membership
        {
            MembershipId = Guid.CreateVersion7(),
            OrganizationId = organizationId,
            UserAccountId = userAccountId,
            OrgRole = role,
            JoinedAt = now
        };
    }

    internal void ChangeRole(OrgRole role) => OrgRole = role;
}
