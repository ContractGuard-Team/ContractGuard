namespace ContractGuard.Domain.Analysis;

// Finding data as received from the worker, after schema and consistency validation.
public sealed record FindingData(
    string FindingKey,
    FindingKind Kind,
    string Title,
    RiskDomain PrimaryDomain,
    List<RiskDomain> SecondaryDomains,
    string? Category,
    Severity Severity,
    string? ClauseKey,
    List<Evidence> Evidence,
    string? MissingDescription,
    string Why,
    string Impact,
    string Recommendation,
    List<FindingReference> References,
    string? CheckCode);
