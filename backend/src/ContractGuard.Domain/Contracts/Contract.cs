using ContractGuard.Domain.Common;

namespace ContractGuard.Domain.Contracts;

// Aggregate root of a contract in the library (ENT-DATA-005). Owns its versions and their contexts.
public sealed class Contract
{
    public const int NameMaxLength = 300;

    // User transitions of SRS §13.2 (TR-CTR-004, 005, 006, 008, 009).
    // System transitions (start, complete, fail analysis) have their own methods.
    private static readonly Dictionary<ApprovalStatus, ApprovalStatus[]> UserTransitions = new()
    {
        [ApprovalStatus.PendingReview] = [ApprovalStatus.Negotiating, ApprovalStatus.Approved, ApprovalStatus.Rejected],
        [ApprovalStatus.Negotiating] = [ApprovalStatus.Approved, ApprovalStatus.Rejected],
        [ApprovalStatus.Approved] = [ApprovalStatus.Negotiating],
        [ApprovalStatus.Rejected] = [ApprovalStatus.Negotiating],
    };

    private readonly List<ContractVersion> _versions = [];

    private Contract()
    {
    }

    public Guid ContractId { get; private set; }

    // Null means a personal contract of the owner (BR-ORG-004). Never changes (CDC-DATA-006).
    public Guid? OrganizationId { get; private set; }
    public Guid OwnerUserAccountId { get; private set; }
    public string ContractName { get; private set; } = string.Empty;
    public ApprovalStatus ApprovalStatus { get; private set; }

    // Status to return to when an analysis fails (TR-CTR-003).
    public ApprovalStatus? StatusBeforeAnalysis { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public IReadOnlyCollection<ContractVersion> Versions => _versions;

    public ContractVersion LatestVersion => _versions.MaxBy(v => v.VersionNumber)
        ?? throw new InvalidOperationException("A contract always has at least one version.");

    public static Contract Create(string name, Guid? organizationId, Guid ownerUserAccountId, ContractFile file, DateTimeOffset now)
    {
        var contract = new Contract
        {
            ContractId = Guid.CreateVersion7(),
            OrganizationId = organizationId,
            OwnerUserAccountId = ownerUserAccountId,
            ContractName = ValidateName(name),
            ApprovalStatus = ApprovalStatus.New,
            CreatedAt = now,
            UpdatedAt = now
        };
        contract._versions.Add(ContractVersion.Create(contract.ContractId, 1, file, ownerUserAccountId, now));
        return contract;
    }

    public void Rename(string name, DateTimeOffset now)
    {
        ContractName = ValidateName(name);
        UpdatedAt = now;
    }

    // F-CTR-005: a new version cannot be uploaded while an analysis runs (ERR-CTR-003).
    public ContractVersion AddVersion(ContractFile file, Guid uploadedBy, DateTimeOffset now)
    {
        EnsureNotAnalyzing();
        var version = ContractVersion.Create(ContractId, LatestVersion.VersionNumber + 1, file, uploadedBy, now);
        _versions.Add(version);
        UpdatedAt = now;
        return version;
    }

    public ContractVersion GetVersion(Guid contractVersionId) =>
        _versions.SingleOrDefault(v => v.ContractVersionId == contractVersionId)
        ?? throw new DomainException(ErrorCodes.NotFound, "Contract version not found.");

    public void ConfirmContext(Guid contractVersionId, ContextDetails details, DateTimeOffset now)
    {
        EnsureNotAnalyzing();
        GetVersion(contractVersionId).ConfirmContext(details, now);
        UpdatedAt = now;
    }

    // TR-CTR-001 and TR-CTR-007.
    public void MarkAnalysisStarted(Guid contractVersionId, DateTimeOffset now)
    {
        EnsureNotAnalyzing();
        if (GetVersion(contractVersionId).Context is null)
        {
            throw new DomainException(ErrorCodes.ContextIncomplete, "Context must be confirmed before analysis.");
        }

        StatusBeforeAnalysis = ApprovalStatus;
        ApprovalStatus = ApprovalStatus.Analyzing;
        UpdatedAt = now;
    }

    // TR-CTR-002. Idempotent: a duplicate "completed" event does nothing.
    public void MarkAnalysisCompleted(DateTimeOffset now)
    {
        if (ApprovalStatus != ApprovalStatus.Analyzing)
        {
            return;
        }

        ApprovalStatus = ApprovalStatus.PendingReview;
        StatusBeforeAnalysis = null;
        UpdatedAt = now;
    }

    // TR-CTR-003: back to the status right before the analysis.
    public void MarkAnalysisFailed(DateTimeOffset now)
    {
        if (ApprovalStatus != ApprovalStatus.Analyzing)
        {
            return;
        }

        ApprovalStatus = StatusBeforeAnalysis ?? ApprovalStatus.New;
        StatusBeforeAnalysis = null;
        UpdatedAt = now;
    }

    // FR-CTR-013: any transition not listed in §13.2 is rejected (ERR-CTR-001).
    public void ChangeApprovalStatus(ApprovalStatus target, DateTimeOffset now)
    {
        if (!UserTransitions.TryGetValue(ApprovalStatus, out var allowed) || !allowed.Contains(target))
        {
            throw new DomainException(
                ErrorCodes.InvalidStatusTransition,
                $"Cannot change approval status from {ApprovalStatus} to {target}.");
        }

        ApprovalStatus = target;
        UpdatedAt = now;
    }

    // FR-SHARE-008, FR-ORG-011. The contract never changes workspace (BR-ORG-004).
    public void TransferOwnership(Guid newOwnerUserAccountId, DateTimeOffset now)
    {
        OwnerUserAccountId = newOwnerUserAccountId;
        UpdatedAt = now;
    }

    private void EnsureNotAnalyzing()
    {
        if (ApprovalStatus == ApprovalStatus.Analyzing)
        {
            throw new DomainException(ErrorCodes.ContractBusy, "The contract is being analyzed.");
        }
    }

    private static string ValidateName(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        name = name.Trim();
        ArgumentOutOfRangeException.ThrowIfGreaterThan(name.Length, NameMaxLength, nameof(name));
        return name;
    }
}
