namespace ContractGuard.Domain.Contracts;

// One uploaded file of a contract: V1, V2, ... (ENT-DATA-006). The file never changes after upload.
public sealed class ContractVersion
{
    public const int FileNameMaxLength = 255;

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
}
