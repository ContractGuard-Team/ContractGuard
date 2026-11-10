namespace ContractGuard.Domain.Analysis;

// A verbatim quote of a clause backing a finding (BR-RISK-013).
public sealed record Evidence(string ClauseKey, int Page, string Quote);
