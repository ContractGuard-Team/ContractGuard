namespace ContractGuard.Domain.Analysis;

// BR-RISK-007: status of one of the 20 checks.
public enum CheckStatus
{
    Pass,
    Partial,
    Missing,
    NotApplicable
}
