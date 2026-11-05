using ContractGuard.Domain.Analysis;
using ContractGuard.Infrastructure.Persistence.Conventions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ContractGuard.Infrastructure.Persistence.Configurations;

public sealed class ClauseConfiguration : IEntityTypeConfiguration<Clause>
{
    public void Configure(EntityTypeBuilder<Clause> builder)
    {
        builder.ToTable("clause");
        builder.Property(c => c.ClauseId).ValueGeneratedNever();
        builder.Property(c => c.ClauseKey).HasMaxLength(Clause.ClauseKeyMaxLength);
        builder.Property(c => c.ClauseNumber).HasMaxLength(Clause.ClauseNumberMaxLength);
        builder.Property(c => c.Heading).HasMaxLength(Clause.HeadingMaxLength);
        builder.Property(c => c.ClauseType).HasMaxLength(3);

        // Page and coordinates are stored together in one jsonb column (ATTR-DATA-035).
        builder.Property(c => c.Location).HasJsonColumn();

        builder.HasIndex(c => new { c.AnalysisRunId, c.ClauseKey }).IsUnique();
    }
}
