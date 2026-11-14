using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Amazon.S3;
using Amazon.S3.Model;
using ContractGuard.Application.Abstractions;
using ContractGuard.Infrastructure.Messaging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace ContractGuard.Infrastructure.Storage;

// Adapter: implements the Application port IObjectStorage with Amazon S3.
internal sealed class S3ObjectStorage(
    IAmazonS3 s3,
    [FromKeyedServices(S3ObjectStorage.BrowserClientKey)] IAmazonS3 browserS3,
    IOptions<StorageOptions> storage) : IObjectStorage
{
    // The client whose address the browser can reach (AwsOptions.PublicServiceUrl).
    public const string BrowserClientKey = "browser";

    private string Bucket => storage.Value.BucketName;

    public async Task PutJsonAsync<T>(string key, T value, CancellationToken cancellationToken)
    {
        await s3.PutObjectAsync(
            new PutObjectRequest
            {
                BucketName = Bucket,
                Key = key,
                ContentType = "application/json",
                ContentBody = JsonSerializer.Serialize(value, MessageJson.Options)
            },
            cancellationToken);
    }

    public async Task<Stream> OpenReadAsync(string key, CancellationToken cancellationToken)
    {
        try
        {
            var response = await s3.GetObjectAsync(Bucket, key, cancellationToken);
            return response.ResponseStream;
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            throw new StoredObjectNotFoundException(key);
        }
    }

    public async Task<PresignedUpload> CreateUploadAsync(string key, long maxSizeBytes, TimeSpan expiresIn, CancellationToken cancellationToken)
    {
        var response = await browserS3.CreatePresignedPostAsync(new CreatePresignedPostRequest
        {
            BucketName = Bucket,
            Key = key,
            Expires = DateTime.UtcNow.Add(expiresIn),
            Conditions = [S3PostCondition.ContentLengthRange(1, maxSizeBytes)]
        });
        return new PresignedUpload(response.Url, response.Fields);
    }

    public Task<string> CreateDownloadUrlAsync(string key, string fileName, string contentType, TimeSpan expiresIn, CancellationToken cancellationToken)
    {
        var request = new GetPreSignedUrlRequest
        {
            BucketName = Bucket,
            Key = key,
            Verb = HttpVerb.GET,
            Expires = DateTime.UtcNow.Add(expiresIn),

            // The SDK signs https links by default, whatever the service address. LocalStack only speaks http.
            Protocol = browserS3.Config.ServiceURL?.StartsWith("http://", StringComparison.OrdinalIgnoreCase) == true ? Protocol.HTTP : Protocol.HTTPS
        };
        request.ResponseHeaderOverrides.ContentType = contentType;

        // FileNameStar keeps a Vietnamese file name intact (RFC 5987).
        request.ResponseHeaderOverrides.ContentDisposition = new ContentDispositionHeaderValue("attachment") { FileNameStar = fileName }.ToString();
        return browserS3.GetPreSignedURLAsync(request);
    }

    public async Task<long?> GetSizeAsync(string key, CancellationToken cancellationToken)
    {
        try
        {
            var metadata = await s3.GetObjectMetadataAsync(Bucket, key, cancellationToken);
            return metadata.ContentLength;
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public async Task<byte[]> ReadStartAsync(string key, int count, CancellationToken cancellationToken)
    {
        using var response = await s3.GetObjectAsync(
            new GetObjectRequest { BucketName = Bucket, Key = key, ByteRange = new ByteRange(0, count - 1) },
            cancellationToken);
        using var buffer = new MemoryStream(count);
        await response.ResponseStream.CopyToAsync(buffer, cancellationToken);
        return buffer.ToArray();
    }

    public async Task PutBytesAsync(string key, byte[] content, string contentType, CancellationToken cancellationToken)
    {
        using var stream = new MemoryStream(content, writable: false);
        await s3.PutObjectAsync(new PutObjectRequest { BucketName = Bucket, Key = key, ContentType = contentType, InputStream = stream }, cancellationToken);
    }

    public Task DeleteAsync(string key, CancellationToken cancellationToken) =>
        s3.DeleteObjectAsync(Bucket, key, cancellationToken);
}
