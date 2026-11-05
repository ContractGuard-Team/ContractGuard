using Amazon.S3;
using Amazon.S3.Model;
using ContractGuard.Infrastructure.Storage;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace ContractGuard.Infrastructure.Aws;

// Readiness: the bucket exists and the API may list it.
internal sealed class S3BucketHealthCheck(IAmazonS3 s3, IOptions<StorageOptions> storage) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            await s3.ListObjectsV2Async(
                new ListObjectsV2Request { BucketName = storage.Value.BucketName, MaxKeys = 1 },
                cancellationToken);
            return HealthCheckResult.Healthy();
        }
        catch (AmazonS3Exception ex)
        {
            return HealthCheckResult.Unhealthy($"Bucket '{storage.Value.BucketName}' is not reachable: {ex.ErrorCode}");
        }
    }
}
