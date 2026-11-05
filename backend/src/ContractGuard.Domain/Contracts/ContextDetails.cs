namespace ContractGuard.Domain.Contracts;

// What the user declares about a version before analysis (ENT-DATA-007, FR-CTR-005).
public sealed record ContextDetails(
    OwnParty OwnParty,
    ContractType ContractType,
    bool HasPersonalData,
    string? CounterpartyName,
    string? CounterpartyOffering,
    ServiceCriticality? ServiceCriticality);
