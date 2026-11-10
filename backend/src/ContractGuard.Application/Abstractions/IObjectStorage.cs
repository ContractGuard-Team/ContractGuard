namespace ContractGuard.Application.Abstractions;

// File storage (S3 on AWS, LocalStack locally). Keys follow SD-07:
// quarantine/..., clean/{workspace}/{contract}/{version}/..., results/{job_id}/...
public interface IObjectStorage
{
    Task PutJsonAsync<T>(string key, T value, CancellationToken cancellationToken);

    // Server-side writes of a whole file (the development seed). Users upload through CreateUploadAsync.
    Task PutBytesAsync(string key, byte[] content, string contentType, CancellationToken cancellationToken);

    // Throws StoredObjectNotFoundException when the key does not exist.
    Task<Stream> OpenReadAsync(string key, CancellationToken cancellationToken);

    // Lets the browser upload one file straight to storage (SD-08). The storage itself refuses
    // an empty file or one above maxSizeBytes, and accepts only this key.
    Task<PresignedUpload> CreateUploadAsync(string key, long maxSizeBytes, TimeSpan expiresIn, CancellationToken cancellationToken);

    // A link the browser opens to read one file. Storage answers it with this content type and, when
    // the link is opened directly, saves the file under fileName instead of showing it.
    Task<string> CreateDownloadUrlAsync(string key, string fileName, string contentType, TimeSpan expiresIn, CancellationToken cancellationToken);

    // Size in bytes, or null when the key does not exist.
    Task<long?> GetSizeAsync(string key, CancellationToken cancellationToken);

    // The first bytes of the object, at most count. The object must exist and not be empty.
    Task<byte[]> ReadStartAsync(string key, int count, CancellationToken cancellationToken);

    Task DeleteAsync(string key, CancellationToken cancellationToken);
}
