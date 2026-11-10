using ContractGuard.Domain.Workspaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ContractGuard.Infrastructure.Persistence.Configurations;

public sealed class InvitationConfiguration : IEntityTypeConfiguration<Invitation>
{
    public void Configure(EntityTypeBuilder<Invitation> builder)
    {
        builder.ToTable("invitation");
        builder.Property(i => i.InvitationId).ValueGeneratedNever();
        builder.Property(i => i.Email).HasMaxLength(256);
        builder.Property(i => i.TokenHash).HasMaxLength(64);

        builder.HasOne<Organization>().WithMany().HasForeignKey(i => i.OrganizationId).OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(i => i.TokenHash).IsUnique();

        // One open invitation per email and organization; inviting again replaces it.
        builder.HasIndex(i => new { i.OrganizationId, i.Email }).IsUnique();
    }
}
