namespace ContractGuard.Application.Abstractions;

// File storage (S3 on AWS, LocalStack locally). Keys follow SD-07:
// quarantine/..., clean/{workspace}/{contract}/{version}/..., results/{job_id}/...
public interface IObjectStorage
{
    Task PutJsonAsync<T>(string key, T value, CancellationToken cancellationToken);

    // Throws StoredObjectNotFoundException when the key does not exist.
    Task<Stream> OpenReadAsync(string key, CancellationToken cancellationToken);
}
