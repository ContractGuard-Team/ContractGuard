namespace ContractGuard.Domain.Analysis;

// BR-RISK-010 thresholds and the FR-RISK-022 floor.
// shortcut: initial values of the SRS, hard-coded; move to the versioned scoring config when eval calibrates them.
public static class RiskLevels
{
    public const int CriticalFindingFloor = 55;

    public static RiskLevel FromScore(int score) => score switch
    {
        < 30 => RiskLevel.Low,
        < 55 => RiskLevel.Medium,
        < 85 => RiskLevel.High,
        _ => RiskLevel.Critical
    };
}
