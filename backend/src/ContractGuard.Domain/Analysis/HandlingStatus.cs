namespace ContractGuard.Domain.Analysis;

// DATA-DATA-012: what the team decided about a finding (F-VIEW-003).
public enum HandlingStatus
{
    Open,
    RiskAccepted,
    NeedsNegotiation,
    FalsePositive,
    Resolved
}
