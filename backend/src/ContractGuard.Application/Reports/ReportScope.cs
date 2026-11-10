namespace ContractGuard.Application.Reports;

// FR-EXP-006: which findings go into the report. Severe = only High and Critical.
public enum ReportScope
{
    All,
    Severe
}
