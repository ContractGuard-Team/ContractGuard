namespace ContractGuard.Application.Analysis;

// Message on analysis-results, as sent by the worker. Untrusted input: validate before use.
public sealed record AnalysisResultMessage(
    string? EventType,
    Guid? JobId,
    string? Step,
    string? ResultKey,
    string? FailureReason,
    string? ErrorCode);
