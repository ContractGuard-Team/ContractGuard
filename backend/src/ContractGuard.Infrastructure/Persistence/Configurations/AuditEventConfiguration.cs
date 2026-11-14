using ContractGuard.Domain.Contracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ContractGuard.Infrastructure.Persistence.Configurations;

public sealed class AuditEventConfiguration : IEntityTypeConfiguration<AuditEvent>
{
    public void Configure(EntityTypeBuilder<AuditEvent> builder)
    {
        builder.ToTable("audit_event");
        builder.Property(e => e.AuditEventId).ValueGeneratedNever();
        builder.Property(e => e.Detail).HasMaxLength(AuditEvent.DetailMaxLength);

        // The log belongs to the contract and is deleted with it (FR-CTR-018).
        builder.HasOne<Contract>().WithMany().HasForeignKey(e => e.ContractId).OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(e => new { e.ContractId, e.OccurredAt });
    }
}
