namespace ContractGuard.Domain.Contracts;

// One entry of a contract's activity log (ENT-DATA-018): who did what, when. Detail is a short
// system-made text (a status name, an email), never contract content.
public sealed class AuditEvent
{
    public const int DetailMaxLength = 300;

    private AuditEvent()
    {
    }

    public Guid AuditEventId { get; private set; }
    public Guid ContractId { get; private set; }
    public Guid UserAccountId { get; private set; }
    public AuditAction Action { get; private set; }
    public string? Detail { get; private set; }
    public DateTimeOffset OccurredAt { get; private set; }

    public static AuditEvent Create(Guid contractId, Guid userAccountId, AuditAction action, string? detail, DateTimeOffset now) => new()
    {
        AuditEventId = Guid.CreateVersion7(),
        ContractId = contractId,
        UserAccountId = userAccountId,
        Action = action,
        Detail = detail is null ? null : detail.Length <= DetailMaxLength ? detail : detail[..DetailMaxLength],
        OccurredAt = now
    };
}
