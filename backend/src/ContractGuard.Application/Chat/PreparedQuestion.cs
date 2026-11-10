using ContractGuard.Domain.Chat;

namespace ContractGuard.Application.Chat;

// A question that passed every check and is saved, with what the model side receives.
public sealed record PreparedQuestion(ChatMessage Question, ChatRequest Request);
