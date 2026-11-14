using ContractGuard.Domain.Chat;

namespace ContractGuard.Application.Chat;

public sealed record ChatTurn(ChatRole Role, string Content);
