namespace ContractGuard.Application.Abstractions;

// Who the current request acts as, and in which workspace (BR-ORG-004).
public interface ICurrentWorkspace
{
    // Throws when there is no user, e.g. in a background job.
    Guid UserId { get; }

    // Null means the personal workspace of the user (FR-ORG-013).
    Guid? OrganizationId { get; }
}
