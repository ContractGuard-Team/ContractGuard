using ContractGuard.Domain.Contracts;
using ContractGuard.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ContractGuard.Infrastructure.Persistence.Configurations;

public sealed class ContractVersionConfiguration : IEntityTypeConfiguration<ContractVersion>
{
    public void Configure(EntityTypeBuilder<ContractVersion> builder)
    {
        builder.ToTable("contract_version");
        builder.Property(v => v.ContractVersionId).ValueGeneratedNever();
        builder.Property(v => v.SourceFileKey).HasMaxLength(1024);
        builder.Property(v => v.OriginalFileName).HasMaxLength(ContractVersion.FileNameMaxLength);

        // V1, V2, ... are unique inside one contract (BR-CTR-003).
        builder.HasIndex(v => new { v.ContractId, v.VersionNumber }).IsUnique();

        builder.HasOne<AppUser>()
            .WithMany()
            .HasForeignKey(v => v.UploadedByUserAccountId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(v => v.Context)
            .WithOne()
            .HasForeignKey<ContractContext>(c => c.ContractVersionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
