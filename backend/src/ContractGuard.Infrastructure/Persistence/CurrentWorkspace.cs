using ContractGuard.Application.Abstractions;

namespace ContractGuard.Infrastructure.Persistence;

// One per DI scope. The query filters of AppDbContext read it (SD-05).
// Deny by default: until a user is set or RunAsSystem() is called, contract data queries return nothing.
public sealed class CurrentWorkspace : ICurrentWorkspace
{
    public bool IsSystem { get; private set; }
    public Guid? CurrentUserId { get; private set; }
    public Guid? OrganizationId { get; private set; }
    public bool IsOrganizationOwner { get; private set; }

    public Guid UserId => CurrentUserId ?? throw new InvalidOperationException("This scope has no current user.");

    public void SetPersonal(Guid userId) => CurrentUserId = userId;

    public void SetOrganization(Guid userId, Guid organizationId, bool isOwner)
    {
        CurrentUserId = userId;
        OrganizationId = organizationId;
        IsOrganizationOwner = isOwner;
    }

    // For background jobs, which act on behalf of the system and see every workspace.
    // Must be called explicitly, so forgetting it shows no data instead of all data.
    public void RunAsSystem() => IsSystem = true;
}
