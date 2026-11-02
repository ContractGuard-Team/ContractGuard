using System.ComponentModel.DataAnnotations;

namespace ContractGuard.Infrastructure.Messaging;

public sealed class QueueOptions
{
    public const string SectionName = "Queues";

    [Required]
    public string AnalysisRequests { get; init; } = string.Empty;
    [Required]
    public string AnalysisResults { get; init; } = string.Empty;
}