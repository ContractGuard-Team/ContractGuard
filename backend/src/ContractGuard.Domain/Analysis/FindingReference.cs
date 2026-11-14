namespace ContractGuard.Domain.Analysis;

// A standard or law cited by a finding, e.g. NIST SP 800-161r1 SR-3.
public sealed record FindingReference(string Source, string ReferenceNumber);
