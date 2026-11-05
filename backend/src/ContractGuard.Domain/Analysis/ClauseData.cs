namespace ContractGuard.Domain.Analysis;

// Clause data as received from the worker, after schema validation.
public sealed record ClauseData(
    string ClauseKey,
    int ClauseOrder,
    string ClauseNumber,
    string? Heading,
    string ClauseText,
    string ClauseType,
    ClauseLocation Location);
