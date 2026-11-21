namespace ContractGuard.Application.Analysis;

// Event types carried by SQS messages (SD-11). Draft agreed in contracts/queue-messages.v1.schema.json.
public static class AnalysisEventTypes
{
    // C# -> worker (analysis-requests)
    public const string AnalysisRequested = "analysis_requested";
    public const string Ping = "ping";

    // C# -> worker (chat-requests)
    public const string ChatRequested = "chat_requested";

    // worker -> C# (analysis-results). "failed" also ends a chat question when job_id is its id.
    public const string Progress = "progress";
    public const string Completed = "completed";
    public const string Failed = "failed";
    public const string Pong = "pong";
    public const string ChatAnswered = "chat_answered";
}
