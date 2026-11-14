namespace ContractGuard.Domain.Contracts;

// FR-AUD-001: what is written to a contract's activity log.
public enum AuditAction
{
    Uploaded,
    Viewed,
    VersionUploaded,
    ContextConfirmed,
    MetadataEdited,
    StatusChanged,
    FindingHandled,
    Shared,
    ShareRevoked,
    OwnerTransferred,
    ReportExported
}
