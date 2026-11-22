using ContractGuard.Domain.Analysis;
using ContractGuard.Domain.Common;
using ContractGuard.Domain.Contracts;

namespace ContractGuard.Api.Endpoints;

// Request and response bodies of ContractEndpoints that are not Application read models.
internal sealed record UploadRequest(string FileName, long SizeBytes);

internal sealed record CreateContractRequest(Guid UploadId, string FileName);

internal sealed record JobStarted(Guid JobId);

// Every field is optional in the JSON so that a missing one reaches the check below and is answered
// with ERR-CTR-002 naming the field, not with a generic 400. Without the nullable types a missing
// own_party would silently become the first enum value, "buyer".
internal sealed record ContextRequest(
    OwnParty? OwnParty = null,
    ContractType? ContractType = null,
    bool? HasPersonalData = null,
    string? CounterpartyName = null,
    string? CounterpartyOffering = null,
    ServiceCriticality? ServiceCriticality = null)
{
    public ContextDetails ToDetails()
    {
        // VAL-DATA-010
        string?[] missing =
        [
            OwnParty is null ? nameof(OwnParty) : null,
            ContractType is null ? nameof(ContractType) : null,
            HasPersonalData is null ? nameof(HasPersonalData) : null
        ];
        if (missing.Any(m => m is not null))
        {
            throw new DomainException(ErrorCodes.ContextIncomplete, $"Missing required fields: {string.Join(", ", missing.OfType<string>())}.");
        }

        if (CounterpartyName?.Trim().Length > ContractContext.CounterpartyNameMaxLength
            || CounterpartyOffering?.Trim().Length > ContractContext.CounterpartyOfferingMaxLength)
        {
            throw new DomainException(ErrorCodes.Validation, "A context field is too long.");
        }

        return new ContextDetails(OwnParty!.Value, ContractType!.Value, HasPersonalData!.Value, CounterpartyName, CounterpartyOffering, ServiceCriticality);
    }
}

// FR-CTR-007: the whole set of metadata fields; a null field is cleared.
internal sealed record MetadataRequest(
    List<string>? Parties = null,
    ContractType? ContractType = null,
    decimal? ContractValue = null,
    string? Currency = null,
    DateOnly? EffectiveDate = null,
    DateOnly? ExpiryDate = null,
    string? GoverningLaw = null)
{
    public ContractMetadata ToMetadata() =>
        new(Parties ?? [], ContractType, ContractValue, string.IsNullOrWhiteSpace(Currency) ? null : Currency.Trim().ToUpperInvariant(), EffectiveDate, ExpiryDate, GoverningLaw);
}

internal sealed record ApprovalStatusRequest(ApprovalStatus Status);

internal sealed record RenameRequest(string Name);

internal sealed record ShareRequest(string? Email = null, bool WholeOrganization = false);

internal sealed record TransferOwnerRequest(Guid UserId);

internal sealed record FindingHandlingRequest(HandlingStatus Status, string? Note = null);
