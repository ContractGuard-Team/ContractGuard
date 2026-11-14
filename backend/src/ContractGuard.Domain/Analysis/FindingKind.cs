namespace ContractGuard.Domain.Analysis;

// BR-RISK-013: a finding is about a clause that says something risky, or about a clause that is missing.
public enum FindingKind
{
    ProblematicClause,
    MissingClause
}
