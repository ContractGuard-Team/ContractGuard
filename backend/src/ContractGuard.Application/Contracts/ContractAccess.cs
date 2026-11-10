using ContractGuard.Application.Abstractions;
using ContractGuard.Domain.Common;
using ContractGuard.Domain.Contracts;

namespace ContractGuard.Application.Contracts;

// SRS §12.3 on top of the query filters. The filters decide who sees a contract (Owner or Member);
// this decides the few things only an Owner does: share, revoke, transfer, delete, read the log.
public static class ContractAccess
{
    // BR-SHARE-002, FR-SHARE-002: the contract owner, or an owner of the contract's organization while
    // working in that organization. Owner-of-organization rights never reach personal contracts.
    public static bool IsOwner(Contract contract, ICurrentWorkspace workspace) =>
        contract.OwnerUserAccountId == workspace.UserId
        || (contract.OrganizationId is not null && contract.OrganizationId == workspace.OrganizationId && workspace.IsOrganizationOwner);

    public static void EnsureOwner(Contract contract, ICurrentWorkspace workspace)
    {
        if (!IsOwner(contract, workspace))
        {
            throw new DomainException(ErrorCodes.Forbidden, "Only the owner of the contract can do this.");
        }
    }
}
