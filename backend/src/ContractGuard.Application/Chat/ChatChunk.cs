using ContractGuard.Domain.Chat;

namespace ContractGuard.Application.Chat;

// One piece of a streamed answer: some text, or a citation.
public sealed record ChatChunk(string? Text, ChatCitation? Citation)
{
    public static ChatChunk Of(string text) => new(text, null);

    public static ChatChunk Cite(string clauseKey, string? quote = null) => new(null, new ChatCitation(clauseKey, quote));
}
