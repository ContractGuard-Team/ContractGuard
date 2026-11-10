namespace ContractGuard.Domain.Chat;

// One question or answer in the conversation of a contract (ENT-DATA-017). The conversation belongs to
// the contract and is shared by everyone who can see it (FR-CHAT-011). Deleted with the contract (FR-CTR-018).
public sealed class ChatMessage
{
    public const int QuestionMaxLength = 2000;
    public const int AuthorNameMaxLength = 100;

    private ChatMessage()
    {
    }

    public Guid ChatMessageId { get; private set; }
    public Guid ContractId { get; private set; }

    // The version open when the question was asked: answers only use that version (BR-CHAT-001).
    public Guid ContractVersionId { get; private set; }
    public ChatRole Role { get; private set; }

    // Who asked; null for answers. The name is kept as it was, like a signature on a note.
    public Guid? AuthorUserAccountId { get; private set; }
    public string? AuthorName { get; private set; }
    public string Content { get; private set; } = string.Empty;
    public List<ChatCitation> Citations { get; private set; } = [];
    public DateTimeOffset CreatedAt { get; private set; }

    public static ChatMessage Question(Guid contractId, Guid contractVersionId, Guid userId, string authorName, string text, DateTimeOffset now)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(text);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(text.Length, QuestionMaxLength);
        return new ChatMessage
        {
            ChatMessageId = Guid.CreateVersion7(),
            ContractId = contractId,
            ContractVersionId = contractVersionId,
            Role = ChatRole.User,
            AuthorUserAccountId = userId,
            AuthorName = authorName.Length <= AuthorNameMaxLength ? authorName : authorName[..AuthorNameMaxLength],
            Content = text,
            CreatedAt = now
        };
    }

    public static ChatMessage Answer(Guid contractId, Guid contractVersionId, string text, List<ChatCitation> citations, DateTimeOffset now) => new()
    {
        ChatMessageId = Guid.CreateVersion7(),
        ContractId = contractId,
        ContractVersionId = contractVersionId,
        Role = ChatRole.Assistant,
        Content = text,
        Citations = citations,
        CreatedAt = now
    };
}
