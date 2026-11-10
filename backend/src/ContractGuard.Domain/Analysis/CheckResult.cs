namespace ContractGuard.Domain.Analysis;

// One of the 20 checks of a run (F-RISK-002). ClauseKeys and FindingKey use the worker's ids.
public sealed record CheckResult(string CheckCode, CheckStatus Status, List<string> ClauseKeys, string? FindingKey, string? Note);
