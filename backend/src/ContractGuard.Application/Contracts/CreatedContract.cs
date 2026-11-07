namespace ContractGuard.Application.Contracts;

// Ids of a contract just created from an upload, with its version V1.
public sealed record CreatedContract(Guid ContractId, Guid ContractVersionId);
