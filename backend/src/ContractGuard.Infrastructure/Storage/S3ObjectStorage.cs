using System.Net;
using System.Text.Json;
using Amazon.S3;
using Amazon.S3.Model;
using ContractGuard.Application.Abstractions;
using ContractGuard.Infrastructure.Messaging;
using Microsoft.Extensions.Options;

namespace ContractGuard.Infrastructure.Storage;

// Adapter: implements the Application port IObjectStorage with Amazon S3.
internal sealed class S3ObjectStorage(IAmazonS3 s3, IOptions<StorageOptions> storage) : IObjectStorage
{
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
}
