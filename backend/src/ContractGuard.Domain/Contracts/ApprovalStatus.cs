namespace ContractGuard.Domain.Contracts;

// DATA-DATA-003. Stored as lower snake_case: new, analyzing, pending_review, ...
public enum ApprovalStatus
{
    New,
    Analyzing,
    PendingReview,
    Negotiating,
    Approved,
    Rejected
}
