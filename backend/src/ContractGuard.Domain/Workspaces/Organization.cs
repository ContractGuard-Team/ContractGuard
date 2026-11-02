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
}
