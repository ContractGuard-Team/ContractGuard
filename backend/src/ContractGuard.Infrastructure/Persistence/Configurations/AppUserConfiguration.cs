using ContractGuard.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ContractGuard.Infrastructure.Persistence.Configurations;

public sealed class AppUserConfiguration : IEntityTypeConfiguration<AppUser>
{
    public void Configure(EntityTypeBuilder<AppUser> builder)
    {
        builder.ToTable("user_account");
        builder.Property(u => u.Id).HasColumnName("user_account_id");

        builder.HasIndex(u => u.NormalizedEmail)
            .IsUnique()
            .HasDatabaseName("ix_user_account_normalized_email");
        builder.HasIndex(u => u.NormalizedUserName)
            .IsUnique()
            .HasDatabaseName("ix_user_account_normalized_user_name");
    }
}
