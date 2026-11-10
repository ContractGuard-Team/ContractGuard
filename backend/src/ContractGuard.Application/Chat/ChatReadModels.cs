using ContractGuard.Domain.Chat;

namespace ContractGuard.Application.Chat;

// The conversation of a contract as the chat panel reads it, kept together like ContractReadModels.
public sealed record ChatThread(bool Available, List<ChatMessageItem> Messages);

public sealed record ChatMessageItem(
    Guid ChatMessageId,
    ChatRole Role,
    string? AuthorName,
    string Content,
    List<ChatCitation> Citations,
    int VersionNumber,
    DateTimeOffset CreatedAt);
