namespace ContractGuard.Domain.Analysis;

// One run of the pipeline on one contract version (ENT-DATA-010).
// AnalysisRunId doubles as the job_id carried by SQS messages (SD-11).
// C# owns this state machine; the worker only reports events (SD-13).
public sealed class AnalysisRun
{
    public const int FailureReasonMaxLength = 2000;
    public const int VersionTextMaxLength = 100;

    private readonly List<Clause> _clauses = [];

    private AnalysisRun()
    {
    }

    public Guid AnalysisRunId { get; private set; }
    public Guid ContractId { get; private set; }
    public Guid ContractVersionId { get; private set; }
    public RunStatus RunStatus { get; private set; }
    public AnalysisStep CurrentStep { get; private set; }
    public string? FailureReason { get; private set; }

    // Written into every log line of this run (FR-ANA-017).
    public Guid CorrelationId { get; private set; }
    public string? SchemaVersion { get; private set; }
    public string? ModelVersion { get; private set; }
    public string? PromptVersion { get; private set; }
    public string? RuleVersion { get; private set; }
    public DateTimeOffset StartedAt { get; private set; }

    // Last time the worker said anything. Used to fail silent jobs after 20 minutes (SD-13).
    public DateTimeOffset LastEventAt { get; private set; }
    public DateTimeOffset? FinishedAt { get; private set; }
    public IReadOnlyCollection<Clause> Clauses => _clauses;

    public bool IsTerminal => RunStatus != RunStatus.Running;

    public static AnalysisRun Start(Guid contractId, Guid contractVersionId, DateTimeOffset now)
    {
        var id = Guid.CreateVersion7();
        return new AnalysisRun
        {
            AnalysisRunId = id,
            ContractId = contractId,
            ContractVersionId = contractVersionId,
            RunStatus = RunStatus.Running,
            CurrentStep = AnalysisStep.ReadingFile,
            CorrelationId = id,
            StartedAt = now,
            LastEventAt = now
        };
    }

    // Messages can arrive twice or out of order (SQS is at-least-once), so progress never goes back.
    public bool ReportProgress(AnalysisStep step, DateTimeOffset now)
    {
        if (IsTerminal)
        {
            return false;
        }

        LastEventAt = now;
        if (step > CurrentStep)
        {
            CurrentStep = step;
        }

        return true;
    }

    // TR-ANA-002. Returns false when the run is already terminal (duplicate or late message).
    public bool Complete(AnalysisTrace trace, IEnumerable<ClauseData> clauses, DateTimeOffset now)
    {
        if (IsTerminal)
        {
            return false;
        }

        _clauses.Clear();
        _clauses.AddRange(clauses.Select(c => Clause.Create(AnalysisRunId, c)));
        SchemaVersion = Limit(trace.SchemaVersion);
        ModelVersion = Limit(trace.ModelVersion);
        PromptVersion = Limit(trace.PromptVersion);
        RuleVersion = Limit(trace.RuleVersion);
        RunStatus = RunStatus.Completed;
        CurrentStep = AnalysisStep.Completed;
        LastEventAt = now;
        FinishedAt = now;
        return true;
    }

    // TR-ANA-003. Intermediate results are dropped (FR-ANA-005).
    public bool Fail(string reason, DateTimeOffset now)
    {
        if (IsTerminal)
        {
            return false;
        }

        _clauses.Clear();
        FailureReason = string.IsNullOrWhiteSpace(reason)
            ? "Unknown error."
            : reason.Length <= FailureReasonMaxLength ? reason : reason[..FailureReasonMaxLength];
        RunStatus = RunStatus.Failed;
        LastEventAt = now;
        FinishedAt = now;
        return true;
    }

    private static string? Limit(string? value) =>
        value is null ? null : value.Length <= VersionTextMaxLength ? value : value[..VersionTextMaxLength];
}
