namespace ContractGuard.Domain.Analysis;

// STATE-ANA-001..004 of SRS §13.1 (Cancelled: CR-ANA-001).
public enum RunStatus
{
    Running,
    Completed,
    Failed,
    Cancelled
}
