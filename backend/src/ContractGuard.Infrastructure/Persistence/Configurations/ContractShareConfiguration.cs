using ContractGuard.Domain.Contracts;
using ContractGuard.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ContractGuard.Infrastructure.Persistence.Configurations;

public sealed class ContractShareConfiguration : IEntityTypeConfiguration<ContractShare>
{
    public void Configure(EntityTypeBuilder<ContractShare> builder)
    {
        builder.ToTable("contract_share");
        builder.Property(s => s.ContractShareId).ValueGeneratedNever();

        builder.HasOne<Contract>().WithMany().HasForeignKey(s => s.ContractId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<AppUser>().WithMany().HasForeignKey(s => s.UserAccountId).OnDelete(DeleteBehavior.Cascade);

        // The contract filter looks shares up by (contract, user) on every query.
        builder.HasIndex(s => new { s.ContractId, s.UserAccountId }).IsUnique();
        builder.HasIndex(s => s.UserAccountId);
    }
}
