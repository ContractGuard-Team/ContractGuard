namespace ContractGuard.Application.Abstractions;

public sealed record UserSummary(Guid UserId, string DisplayName, string Email);
