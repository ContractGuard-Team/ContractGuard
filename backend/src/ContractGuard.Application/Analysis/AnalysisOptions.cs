namespace ContractGuard.Application.Analysis;

public sealed class AnalysisOptions
{
    public const string SectionName = "Analysis";

    // False while no AI worker is deployed: confirming a context only saves it, because a job would
    // sit on the queue until the sweeper fails it 20 minutes later.
    public bool WorkerEnabled { get; init; } = true;
}
