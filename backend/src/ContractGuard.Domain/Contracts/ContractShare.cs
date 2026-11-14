namespace ContractGuard.Domain.Contracts;

// A Member role of one account on one contract (ENT-DATA-009, BR-SHARE-002). Deleting the row is
// the revocation, which applies to the next request (BR-SHARE-004).
public sealed class ContractShare
{
    private ContractShare()
    {
    }

    public Guid ContractShareId { get; private set; }
    public Guid ContractId { get; private set; }
    public Guid UserAccountId { get; private set; }
    public Guid SharedByUserAccountId { get; private set; }
    public DateTimeOffset SharedAt { get; private set; }

    public static ContractShare Create(Guid contractId, Guid userAccountId, Guid sharedBy, DateTimeOffset now) => new()
    {
        ContractShareId = Guid.CreateVersion7(),
        ContractId = contractId,
        UserAccountId = userAccountId,
        SharedByUserAccountId = sharedBy,
        SharedAt = now
    };
}
