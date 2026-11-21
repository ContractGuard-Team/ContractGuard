namespace ContractGuard.Application.Chat;

// The worker's answer from a chat_answered message. Untrusted: citations are checked by ChatService.
public sealed record WorkerChatAnswer(string Text, IReadOnlyList<string> CitedClauseIds);
