using ContractGuard.Domain.Contracts;

namespace ContractGuard.Domain.Analysis;

// F-CTR-002: metadata of a version. Every field may be unknown.
public sealed record ContractMetadata(
    List<string> Parties,
    ContractType? ContractType,
    decimal? ContractValue,
    string? Currency,
    DateOnly? EffectiveDate,
    DateOnly? ExpiryDate,
    string? GoverningLaw);
