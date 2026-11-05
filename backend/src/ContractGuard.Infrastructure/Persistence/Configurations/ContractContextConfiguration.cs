using ContractGuard.Domain.Contracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ContractGuard.Infrastructure.Persistence.Configurations;

public sealed class ContractContextConfiguration : IEntityTypeConfiguration<ContractContext>
{
    public void Configure(EntityTypeBuilder<ContractContext> builder)
    {
        builder.ToTable("contract_context");
        builder.Property(c => c.ContractContextId).ValueGeneratedNever();
        builder.Property(c => c.CounterpartyName).HasMaxLength(ContractContext.CounterpartyNameMaxLength);
        builder.Property(c => c.CounterpartyOffering).HasMaxLength(ContractContext.CounterpartyOfferingMaxLength);
    }
}
