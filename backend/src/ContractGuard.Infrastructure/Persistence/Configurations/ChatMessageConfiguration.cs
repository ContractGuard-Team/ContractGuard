using ContractGuard.Domain.Chat;
using ContractGuard.Domain.Contracts;
using ContractGuard.Infrastructure.Persistence.Conventions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ContractGuard.Infrastructure.Persistence.Configurations;

public sealed class ChatMessageConfiguration : IEntityTypeConfiguration<ChatMessage>
{
    public void Configure(EntityTypeBuilder<ChatMessage> builder)
    {
        builder.ToTable("chat_message");
        builder.Property(m => m.ChatMessageId).ValueGeneratedNever();
        builder.Property(m => m.AuthorName).HasMaxLength(ChatMessage.AuthorNameMaxLength);
        builder.Property(m => m.Citations).HasJsonColumn();

        // FR-CTR-018: the conversation goes with the contract.
        builder.HasOne<Contract>().WithMany().HasForeignKey(m => m.ContractId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<ContractVersion>().WithMany().HasForeignKey(m => m.ContractVersionId).OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(m => new { m.ContractId, m.CreatedAt });
    }
}
