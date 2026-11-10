namespace ContractGuard.Domain.Analysis;

// BR-RISK-001: the seven risk domains. Stored as legal, ..., data_privacy, third_party.
public enum RiskDomain
{
    Legal,
    Financial,
    Operational,
    Compliance,
    Cybersecurity,
    DataPrivacy,
    ThirdParty
}
