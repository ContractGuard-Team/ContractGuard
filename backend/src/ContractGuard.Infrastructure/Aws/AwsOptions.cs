using System.ComponentModel.DataAnnotations;

namespace ContractGuard.Infrastructure.Aws;

public sealed class AwsOptions
{
    public const string SectionName = "Aws";

    [Required]
    public string Region { get; init; } = string.Empty;

    // Set only for local development: every S3/SQS call goes to LocalStack instead of AWS.
    // Leave empty on AWS so the SDK uses the real regional endpoints.
    public string? ServiceUrl { get; init; }

    // The address a browser uses to reach S3 when it differs from ServiceUrl, e.g. the API runs
    // in Docker and talks to http://localstack:4566 while the browser needs http://localhost:4566.
    // Presigned URLs are signed for this host.
    public string? PublicServiceUrl { get; init; }

    public bool UsesLocalEndpoint => !string.IsNullOrWhiteSpace(ServiceUrl);
}
