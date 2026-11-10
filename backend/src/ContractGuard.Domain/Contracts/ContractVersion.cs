using System.Text.RegularExpressions;
using ContractGuard.Domain.Analysis;
using ContractGuard.Domain.Common;

namespace ContractGuard.Domain.Contracts;

// One uploaded file of a contract: V1, V2, ... (ENT-DATA-006). The file never changes after upload.
public sealed partial class ContractVersion
{
    public const int FileNameMaxLength = 255;
    public const int MaxParties = 20;
    public const int PartyMaxLength = 300;
    public const int GoverningLawMaxLength = 200;

    private ContractVersion()
    {
    }

    public Guid ContractVersionId { get; private set; }
    public Guid ContractId { get; private set; }
    public int VersionNumber { get; private set; }

    // S3 key. Starts in quarantine/, moves to clean/ once the worker has checked the file (SD-07).
    public string SourceFileKey { get; private set; } = string.Empty;
    public string OriginalFileName { get; private set; } = string.Empty;
    public FileType FileType { get; private set; }
    public long FileSizeBytes { get; private set; }

    // Known only after the worker has read the file.
    public int? PageCount { get; private set; }
    public Guid UploadedByUserAccountId { get; private set; }
    public DateTimeOffset UploadedAt { get; private set; }
    public ContractContext? Context { get; private set; }

    // F-CTR-002 (ENT-DATA-008): what the worker extracted, and what a user corrected. A re-run replaces
    // the extracted values but never the corrections, so the effective value is the edit when there is one.
    public ContractMetadata? ExtractedMetadata { get; private set; }
    public ContractMetadata? EditedMetadata { get; private set; }
    public Guid? MetadataEditedByUserAccountId { get; private set; }
    public DateTimeOffset? MetadataEditedAt { get; private set; }

    public ContractMetadata? Metadata => EditedMetadata ?? ExtractedMetadata;

    internal static ContractVersion Create(Guid contractId, int versionNumber, ContractFile file, Guid uploadedBy, DateTimeOffset now)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(file.StorageKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(file.OriginalFileName);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(file.OriginalFileName.Length, FileNameMaxLength);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(file.SizeBytes);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(file.SizeBytes, ContractFile.MaxSizeBytes);

        return new ContractVersion
        {
            ContractVersionId = Guid.CreateVersion7(),
            ContractId = contractId,
            VersionNumber = versionNumber,
            SourceFileKey = file.StorageKey,
            OriginalFileName = file.OriginalFileName,
            FileType = file.FileType,
            FileSizeBytes = file.SizeBytes,
            UploadedByUserAccountId = uploadedBy,
            UploadedAt = now
        };
    }

    internal void ConfirmContext(ContextDetails details, DateTimeOffset now)
    {
        if (Context is null)
        {
            Context = ContractContext.Create(ContractVersionId, details, now);
        }
        else
        {
            Context.Apply(details, now);
        }
    }

    // Called with what the worker found when reading the file (SD-07, SD-08): the real page count
    // and, once the file passed the checks, its sanitized copy under clean/.
    public void RecordDocumentInfo(int pageCount, string? cleanFileKey)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(pageCount);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(pageCount, ContractFile.MaxPageCount);
        PageCount = pageCount;
        if (!string.IsNullOrWhiteSpace(cleanFileKey))
        {
            SourceFileKey = cleanFileKey;
        }
    }

    public void RecordExtractedMetadata(ContractMetadata? metadata) => ExtractedMetadata = metadata;

    // FR-CTR-007. The user sends the whole set of fields; null clears a field.
    public void EditMetadata(ContractMetadata metadata, Guid userId, DateTimeOffset now)
    {
        var parties = metadata.Parties.Select(p => p.Trim()).Where(p => p.Length > 0).ToList();
        if (parties.Count > MaxParties || parties.Any(p => p.Length > PartyMaxLength))
        {
            throw new DomainException(ErrorCodes.Validation, $"At most {MaxParties} parties of {PartyMaxLength} characters.");
        }

        if (metadata.Currency is not null && !CurrencyPattern().IsMatch(metadata.Currency))
        {
            throw new DomainException(ErrorCodes.Validation, "Currency must be a 3-letter ISO 4217 code.");
        }

        if (metadata.ContractValue < 0)
        {
            throw new DomainException(ErrorCodes.Validation, "Contract value cannot be negative.");
        }

        if (metadata.GoverningLaw?.Length > GoverningLawMaxLength)
        {
            throw new DomainException(ErrorCodes.Validation, $"Governing law is at most {GoverningLawMaxLength} characters.");
        }

        EditedMetadata = metadata with
        {
            Parties = parties,
            GoverningLaw = string.IsNullOrWhiteSpace(metadata.GoverningLaw) ? null : metadata.GoverningLaw.Trim()
        };
        MetadataEditedByUserAccountId = userId;
        MetadataEditedAt = now;
    }

    [GeneratedRegex("^[A-Z]{3}$")]
    private static partial Regex CurrencyPattern();
}
