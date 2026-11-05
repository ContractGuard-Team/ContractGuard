using ContractGuard.Domain.Workspaces;
using ContractGuard.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ContractGuard.Infrastructure.Persistence.Configurations;

public sealed class MembershipConfiguration : IEntityTypeConfiguration<Membership>
{
    public void Configure(EntityTypeBuilder<Membership> builder)
    {
        builder.ToTable("membership");
        builder.Property(m => m.MembershipId).ValueGeneratedNever();

        // A user account cannot be deleted while it still belongs to an organization.
        builder.HasOne<AppUser>()
            .WithMany()
            .HasForeignKey(m => m.UserAccountId)
            .OnDelete(DeleteBehavior.Restrict);

        // CDC-DATA-003: at most one membership per (organization, user account).
        builder.HasIndex(m => new { m.OrganizationId, m.UserAccountId }).IsUnique();
    }
}
