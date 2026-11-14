using ContractGuard.Domain.Analysis;
using ContractGuard.Infrastructure.Persistence.Conventions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ContractGuard.Infrastructure.Persistence.Configurations;

public sealed class FindingConfiguration : IEntityTypeConfiguration<Finding>
{
    public void Configure(EntityTypeBuilder<Finding> builder)
    {
        builder.ToTable("finding");
        builder.Property(f => f.FindingId).ValueGeneratedNever();
        builder.Property(f => f.FindingKey).HasMaxLength(Finding.KeyMaxLength);
        builder.Property(f => f.Title).HasMaxLength(Finding.TitleMaxLength);
        builder.Property(f => f.Category).HasMaxLength(Finding.CategoryMaxLength);
        builder.Property(f => f.ClauseKey).HasMaxLength(Clause.ClauseKeyMaxLength);
        builder.Property(f => f.CheckCode).HasMaxLength(5);
        builder.Property(f => f.HandlingNote).HasMaxLength(Finding.NoteMaxLength);

        // Read and written whole with the finding, never queried inside.
        builder.Property(f => f.SecondaryDomains).HasJsonColumn();
        builder.Property(f => f.Evidence).HasJsonColumn();
        builder.Property(f => f.References).HasJsonColumn();

        builder.HasIndex(f => new { f.AnalysisRunId, f.FindingKey }).IsUnique();
    }
}
