namespace ContractGuard.Application.Analysis;

public sealed record ResultProcessingOutcome(IReadOnlyList<string> Errors)
{
    public static readonly ResultProcessingOutcome Success = new([]);
}
