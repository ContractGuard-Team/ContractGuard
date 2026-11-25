using System.ComponentModel.DataAnnotations;

namespace ContractGuard.Infrastructure.Messaging;

public sealed class QueueOptions
{
    public const string SectionName = "Queues";

    [Required]
    public string AnalysisRequests { get; init; } = string.Empty;
    [Required]
    public string AnalysisResults { get; init; } = string.Empty;

    // Chat questions get their own queue: the worker takes one message at a time and an analysis runs for
    // minutes, so a question behind it would time out. Answers come back on AnalysisResults.
    [Required]
    public string ChatRequests { get; init; } = string.Empty;

    // Cancels get their own queue too: the worker reads AnalysisRequests only between two contracts (CR-ANA-001).
    [Required]
    public string AnalysisCancels { get; init; } = string.Empty;
}