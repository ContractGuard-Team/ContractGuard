namespace ContractGuard.Application.Analysis;

// Message on analysis-requests. Small on purpose: details live in the S3 object at RequestKey.
public sealed record AnalysisRequestMessage(string EventType, Guid JobId, string? RequestKey)
{
    public const string MessageVersion = "1";
}
