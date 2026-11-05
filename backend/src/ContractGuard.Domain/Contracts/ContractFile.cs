namespace ContractGuard.Domain.Contracts;

// The uploaded file of one version, as known when the upload is confirmed.
public sealed record ContractFile(string StorageKey, string OriginalFileName, FileType FileType, long SizeBytes)
{
    public const long MaxSizeBytes = 20L * 1024 * 1024; // DATA-DATA-007, SD-08
    public const int MaxPageCount = 100;
}
