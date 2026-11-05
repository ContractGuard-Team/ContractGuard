using System.Text.RegularExpressions;

namespace ContractGuard.Domain.Analysis;

// One clause split from the document (ENT-DATA-011). Belongs to one analysis run.
public sealed partial class Clause
{
    public const int ClauseKeyMaxLength = 64;
    public const int ClauseNumberMaxLength = 50;
    public const int HeadingMaxLength = 500;

    private Clause()
    {
    }

    public Guid ClauseId { get; private set; }
    public Guid AnalysisRunId { get; private set; }

    // The worker's own id for the clause (e.g. "cl-012"). Findings and evidence refer to it.
    public string ClauseKey { get; private set; } = string.Empty;
    public int ClauseOrder { get; private set; }
    public string ClauseNumber { get; private set; } = string.Empty;
    public string? Heading { get; private set; }
    public string ClauseText { get; private set; } = string.Empty;

    // C01..C21, or C99 for "other" (DATA-DATA-009).
    public string ClauseType { get; private set; } = string.Empty;
    public ClauseLocation Location { get; private set; } = new(1, 1, []);

    internal static Clause Create(Guid analysisRunId, ClauseData data)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(data.ClauseKey);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(data.ClauseKey.Length, ClauseKeyMaxLength);
        ArgumentException.ThrowIfNullOrWhiteSpace(data.ClauseText);
        if (!ClauseTypePattern().IsMatch(data.ClauseType))
        {
            throw new ArgumentException($"Unknown clause type '{data.ClauseType}'.", nameof(data));
        }

        return new Clause
        {
            ClauseId = Guid.CreateVersion7(),
            AnalysisRunId = analysisRunId,
            ClauseKey = data.ClauseKey,
            ClauseOrder = data.ClauseOrder,
            ClauseNumber = Truncate(data.ClauseNumber, ClauseNumberMaxLength) ?? string.Empty,
            Heading = Truncate(data.Heading, HeadingMaxLength),
            ClauseText = data.ClauseText,
            ClauseType = data.ClauseType,
            Location = data.Location
        };
    }

    private static string? Truncate(string? value, int maxLength) =>
        value is null ? null : value.Length <= maxLength ? value : value[..maxLength];

    [GeneratedRegex("^C(0[1-9]|1[0-9]|2[01]|99)$")]
    private static partial Regex ClauseTypePattern();
}
