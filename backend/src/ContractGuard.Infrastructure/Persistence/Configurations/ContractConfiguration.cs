using ContractGuard.Domain.Contracts;
using ContractGuard.Domain.Workspaces;
using ContractGuard.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ContractGuard.Infrastructure.Persistence.Configurations;

public sealed class ContractConfiguration : IEntityTypeConfiguration<Contract>
{
    public void Configure(EntityTypeBuilder<Contract> builder)
    {
        builder.ToTable("contract");
        builder.Property(c => c.ContractId).ValueGeneratedNever();
        builder.Property(c => c.ContractName).HasMaxLength(Contract.NameMaxLength);
        builder.Ignore(c => c.LatestVersion);

        // Organizations cannot be deleted in the MVP (OQ-ORG-002), so RESTRICT is a safety net.
        builder.HasOne<Organization>()
            .WithMany()
            .HasForeignKey(c => c.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<AppUser>()
            .WithMany()
            .HasForeignKey(c => c.OwnerUserAccountId)
            .OnDelete(DeleteBehavior.Restrict);

        // CDC-DATA-002: deleting a contract deletes everything under it.
        builder.HasMany(c => c.Versions)
            .WithOne()
            .HasForeignKey(v => v.ContractId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
