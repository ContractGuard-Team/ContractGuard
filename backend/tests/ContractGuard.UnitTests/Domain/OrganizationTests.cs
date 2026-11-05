using ContractGuard.Domain.Workspaces;

namespace ContractGuard.UnitTests.Domain;

public sealed class OrganizationTests
{
    [Fact]
    public void Creator_becomes_owner()
    {
        var owner = Guid.CreateVersion7();

        var organization = Organization.Create("  Công ty A ", owner, TestData.Now);

        Assert.Equal("Công ty A", organization.OrganizationName);
        var membership = Assert.Single(organization.Memberships);
        Assert.Equal(owner, membership.UserAccountId);
        Assert.Equal(OrgRole.Owner, membership.OrgRole);
        Assert.Equal(organization.OrganizationId, membership.OrganizationId);
    }

    [Fact]
    public void Name_longer_than_limit_is_rejected()
    {
        var name = new string('a', Organization.NameMaxLength + 1);

        Assert.Throws<ArgumentOutOfRangeException>(() => Organization.Create(name, Guid.CreateVersion7(), TestData.Now));
    }
}
