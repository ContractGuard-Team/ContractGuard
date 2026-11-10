namespace ContractGuard.Domain.Chat;

// A clause an answer refers to (FR-CHAT-003), by the worker's clause id of the version's analysis.
public sealed record ChatCitation(string ClauseKey, string? Quote);
