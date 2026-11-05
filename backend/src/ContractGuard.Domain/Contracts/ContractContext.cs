namespace ContractGuard.Domain.Contracts;

// One confirmed context per version (REL-DATA-009). Re-confirming replaces the values.
public sealed class ContractContext
{
    public const int CounterpartyNameMaxLength = 300;
    public const int CounterpartyOfferingMaxLength = 2000;

    private ContractContext()
    {
    }

    public Guid ContractContextId { get; private set; }
    public Guid ContractVersionId { get; private set; }
    public OwnParty OwnParty { get; private set; }
    public string? CounterpartyName { get; private set; }
    public ContractType ContractType { get; private set; }
    public string? CounterpartyOffering { get; private set; }
    public bool HasPersonalData { get; private set; }
    public ServiceCriticality? ServiceCriticality { get; private set; }
    public DateTimeOffset ConfirmedAt { get; private set; }

    internal static ContractContext Create(Guid contractVersionId, ContextDetails details, DateTimeOffset now)
    {
        var context = new ContractContext
        {
            ContractContextId = Guid.CreateVersion7(),
            ContractVersionId = contractVersionId
        };
        context.Apply(details, now);
        return context;
    }

    internal void Apply(ContextDetails details, DateTimeOffset now)
    {
        CounterpartyName = Normalize(details.CounterpartyName, CounterpartyNameMaxLength, nameof(details.CounterpartyName));
        CounterpartyOffering = Normalize(details.CounterpartyOffering, CounterpartyOfferingMaxLength, nameof(details.CounterpartyOffering));
        OwnParty = details.OwnParty;
        ContractType = details.ContractType;
        HasPersonalData = details.HasPersonalData;
        ServiceCriticality = details.ServiceCriticality;
        ConfirmedAt = now;
    }

    private static string? Normalize(string? value, int maxLength, string paramName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        value = value.Trim();
        ArgumentOutOfRangeException.ThrowIfGreaterThan(value.Length, maxLength, paramName);
        return value;
    }
}
