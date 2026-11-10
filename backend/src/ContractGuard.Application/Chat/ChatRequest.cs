using ContractGuard.Domain.Users;

namespace ContractGuard.Application.Chat;

// What the backend hands to the model side. History is the last messages of the same contract version.
public sealed record ChatRequest(
    Guid ContractId,
    Guid ContractVersionId,
    Guid AnalysisRunId,
    string Question,
    IReadOnlyList<ChatTurn> History,
    UiLanguage Language);
