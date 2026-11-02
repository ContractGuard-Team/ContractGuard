using ContractGuard.Domain.Workspaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ContractGuard.Infrastructure.Persistence.Configurations;

public sealed class OrganizationConfiguration : IEntityTypeConfiguration<Organization>
{
    public void Configure(EntityTypeBuilder<Organization> builder)
    {
        builder.ToTable("organization");
        builder.Property(o => o.OrganizationId).ValueGeneratedNever();
        builder.Property(o => o.OrganizationName).HasMaxLength(Organization.NameMaxLength);

        builder.HasMany(o => o.Memberships)
            .WithOne()
            .HasForeignKey(m => m.OrganizationId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
