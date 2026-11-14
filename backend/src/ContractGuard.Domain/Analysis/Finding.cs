namespace ContractGuard.Domain.Analysis;

// One risk found by an analysis run (ENT-DATA-014, BR-RISK-014). Content comes from the worker and
// never changes; only the handling status and note are the team's (F-VIEW-003).
public sealed class Finding
{
    public const int KeyMaxLength = 64;
    public const int TitleMaxLength = 300;
    public const int CategoryMaxLength = 200;
    public const int NoteMaxLength = 2000;

    private Finding()
    {
    }

    public Guid FindingId { get; private set; }
    public Guid AnalysisRunId { get; private set; }

    // The worker's own id (e.g. "f-001"). Checks refer to it.
    public string FindingKey { get; private set; } = string.Empty;
    public FindingKind Kind { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public RiskDomain PrimaryDomain { get; private set; }
    public List<RiskDomain> SecondaryDomains { get; private set; } = [];
    public string? Category { get; private set; }
    public Severity Severity { get; private set; }

    // Worker clause id. Null for a missing clause (REL-DATA-016).
    public string? ClauseKey { get; private set; }
    public List<Evidence> Evidence { get; private set; } = [];
    public string? MissingDescription { get; private set; }
    public string Why { get; private set; } = string.Empty;
    public string Impact { get; private set; } = string.Empty;
    public string Recommendation { get; private set; } = string.Empty;
    public List<FindingReference> References { get; private set; } = [];
    public string? CheckCode { get; private set; }

    public HandlingStatus HandlingStatus { get; private set; }
    public string? HandlingNote { get; private set; }
    public Guid? HandledByUserAccountId { get; private set; }
    public DateTimeOffset? HandledAt { get; private set; }

    internal static Finding Create(Guid analysisRunId, FindingData data)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(data.FindingKey);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(data.FindingKey.Length, KeyMaxLength);
        return new Finding
        {
            FindingId = Guid.CreateVersion7(),
            AnalysisRunId = analysisRunId,
            FindingKey = data.FindingKey,
            Kind = data.Kind,
            Title = Truncate(data.Title, TitleMaxLength)!,
            PrimaryDomain = data.PrimaryDomain,
            SecondaryDomains = data.SecondaryDomains,
            Category = Truncate(data.Category, CategoryMaxLength),
            Severity = data.Severity,
            ClauseKey = data.ClauseKey,
            Evidence = data.Evidence,
            MissingDescription = data.MissingDescription,
            Why = data.Why,
            Impact = data.Impact,
            Recommendation = data.Recommendation,
            References = data.References,
            CheckCode = data.CheckCode,
            HandlingStatus = HandlingStatus.Open
        };
    }

    // FR-VIEW-013, FR-VIEW-014. Any status may follow any other: the SRS sets no order.
    public void SetHandling(HandlingStatus status, string? note, Guid userId, DateTimeOffset now)
    {
        note = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
        ArgumentOutOfRangeException.ThrowIfGreaterThan(note?.Length ?? 0, NoteMaxLength, nameof(note));
        HandlingStatus = status;
        HandlingNote = note;
        HandledByUserAccountId = userId;
        HandledAt = now;
    }

    private static string? Truncate(string? value, int maxLength) =>
        value is null ? null : value.Length <= maxLength ? value : value[..maxLength];
}
