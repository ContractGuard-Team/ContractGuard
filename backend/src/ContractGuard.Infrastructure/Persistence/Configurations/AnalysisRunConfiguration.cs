using ContractGuard.Domain.Analysis;
using ContractGuard.Domain.Contracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ContractGuard.Infrastructure.Persistence.Configurations;

public sealed class AnalysisRunConfiguration : IEntityTypeConfiguration<AnalysisRun>
{
    public void Configure(EntityTypeBuilder<AnalysisRun> builder)
    {
        builder.ToTable("analysis_run");
        builder.Property(r => r.AnalysisRunId).ValueGeneratedNever();
        builder.Property(r => r.FailureReason).HasMaxLength(AnalysisRun.FailureReasonMaxLength);
        builder.Property(r => r.SchemaVersion).HasMaxLength(AnalysisRun.VersionTextMaxLength);
        builder.Property(r => r.ModelVersion).HasMaxLength(AnalysisRun.VersionTextMaxLength);
        builder.Property(r => r.PromptVersion).HasMaxLength(AnalysisRun.VersionTextMaxLength);
        builder.Property(r => r.RuleVersion).HasMaxLength(AnalysisRun.VersionTextMaxLength);
        builder.Ignore(r => r.IsTerminal);

        // A run references the contract aggregate by id only (no navigation property).
        builder.HasOne<Contract>()
            .WithMany()
            .HasForeignKey(r => r.ContractId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<ContractVersion>()
            .WithMany()
            .HasForeignKey(r => r.ContractVersionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(r => r.Clauses)
            .WithOne()
            .HasForeignKey(c => c.AnalysisRunId)
            .OnDelete(DeleteBehavior.Cascade);

        // The sweeper looks for running jobs that have been silent for too long (SD-13).
        builder.HasIndex(r => new { r.RunStatus, r.LastEventAt });
    }
}
