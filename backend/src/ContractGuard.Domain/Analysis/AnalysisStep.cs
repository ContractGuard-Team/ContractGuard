namespace ContractGuard.Domain.Analysis;

// The five steps shown to the user (FR-ANA-002). Order matters: progress only moves forward.
public enum AnalysisStep
{
    ReadingFile,
    SplittingClauses,
    Evaluating,
    Scoring,
    Completed
}
