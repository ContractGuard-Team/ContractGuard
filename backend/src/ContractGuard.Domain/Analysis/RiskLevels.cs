namespace ContractGuard.Domain.Analysis;

// FR-RISK-022 floor. Score-to-level thresholds (BR-RISK-010) are no longer checked here: the worker's versioned
// scoring policy sets the levels (docs/04 P7-01).
public static class RiskLevels
{
    public const int CriticalFindingFloor = 55;
}
