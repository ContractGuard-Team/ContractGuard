using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using ContractGuard.Infrastructure.Aws;
using ContractGuard.Infrastructure.Storage;
using Microsoft.Extensions.Options;

namespace ContractGuard.UnitTests.Infrastructure;

// Integration tests replace S3 with a stand-in, so the real link is only built here. No network needed.
public sealed class S3ObjectStorageTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private static S3ObjectStorage StorageWith(IAmazonS3 browserClient) =>
        new(browserClient, browserClient, Options.Create(new StorageOptions { BucketName = "bucket" }));

    private static Task<string> LinkAsync(IAmazonS3 browserClient, string fileName = "a.pdf") =>
        StorageWith(browserClient).CreateDownloadUrlAsync("quarantine/a.pdf", fileName, "application/pdf", TimeSpan.FromMinutes(15), Ct);

    [Fact]
    public async Task Link_to_localstack_uses_http_because_it_has_no_tls()
    {
        using var client = AwsClientFactory.CreateS3(new AwsOptions { Region = "ap-southeast-1" }, "http://localhost:4566");

        Assert.StartsWith("http://localhost:4566/bucket/quarantine/a.pdf?", await LinkAsync(client), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Link_to_aws_uses_https()
    {
        using var client = new AmazonS3Client(new BasicAWSCredentials("test", "test"), RegionEndpoint.APSoutheast1);

        Assert.StartsWith("https://", await LinkAsync(client), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Link_tells_storage_to_serve_the_checked_type_and_keep_the_vietnamese_name()
    {
        using var client = AwsClientFactory.CreateS3(new AwsOptions { Region = "ap-southeast-1" }, "http://localhost:4566");

        var query = Uri.UnescapeDataString(new Uri(await LinkAsync(client, "Hợp đồng.pdf")).Query);

        Assert.Contains("response-content-type=application/pdf", query, StringComparison.Ordinal);
        Assert.Contains($"response-content-disposition=attachment; filename*=utf-8''{Uri.EscapeDataString("Hợp đồng.pdf")}", query, StringComparison.Ordinal);
    }
}
