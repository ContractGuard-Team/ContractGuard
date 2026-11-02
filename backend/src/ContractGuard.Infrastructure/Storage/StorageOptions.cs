using System.ComponentModel.DataAnnotations;

namespace ContractGuard.Infrastructure.Storage;

public sealed class StorageOptions
{
    public const string SectionName = "Storage";

    [Required]
    public string BucketName { get; init; } = string.Empty;
}