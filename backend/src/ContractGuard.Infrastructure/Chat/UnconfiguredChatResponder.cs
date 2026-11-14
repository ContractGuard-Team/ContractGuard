using ContractGuard.Application.Chat;

namespace ContractGuard.Infrastructure.Chat;

// Default until the AI team connects a model (docs/06-ai-integration-points.md): the chat says it is
// not connected instead of inventing answers.
internal sealed class UnconfiguredChatResponder : IChatResponder
{
    public bool IsAvailable => false;

    public IAsyncEnumerable<ChatChunk> AnswerAsync(ChatRequest request, CancellationToken cancellationToken) =>
        throw new InvalidOperationException("No chat model is configured.");
}
