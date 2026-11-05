namespace ContractGuard.Application.Analysis;

// Event types carried by SQS messages (SD-11). Draft agreed in contracts/queue-messages.v1.schema.json.
public static class AnalysisEventTypes
{
    // C# -> worker (analysis-requests)
    public const string AnalysisRequested = "analysis_requested";
    public const string Ping = "ping";

    // worker -> C# (analysis-results)
    public const string Progress = "progress";
    public const string Completed = "completed";
    public const string Failed = "failed";
    public const string Pong = "pong";
}
