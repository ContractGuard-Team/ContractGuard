using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;
using ContractGuard.Application.Abstractions;
using ContractGuard.Infrastructure.Messaging;

namespace ContractGuard.IntegrationTests;

// Stand-in for S3 in tests: a dictionary of key -> bytes.
public sealed class InMemoryObjectStorage : IObjectStorage
{
    private readonly ConcurrentDictionary<string, byte[]> _objects = new();

    public void PutText(string key, string text) => _objects[key] = Encoding.UTF8.GetBytes(text);

    public void PutBytes(string key, byte[] bytes) => _objects[key] = bytes;

    public bool Exists(string key) => _objects.ContainsKey(key);

    public string GetText(string key) => Encoding.UTF8.GetString(_objects[key]);

    public Task PutJsonAsync<T>(string key, T value, CancellationToken cancellationToken)
    {
        _objects[key] = JsonSerializer.SerializeToUtf8Bytes(value, MessageJson.Options);
        return Task.CompletedTask;
    }

    public Task<Stream> OpenReadAsync(string key, CancellationToken cancellationToken) =>
        _objects.TryGetValue(key, out var bytes)
            ? Task.FromResult<Stream>(new MemoryStream(bytes, writable: false))
            : throw new StoredObjectNotFoundException(key);

    public Task<PresignedUpload> CreateUploadAsync(string key, long maxSizeBytes, TimeSpan expiresIn, CancellationToken cancellationToken) =>
        Task.FromResult(new PresignedUpload("http://storage.test/upload", new Dictionary<string, string> { ["key"] = key }));

    public Task<string> CreateDownloadUrlAsync(string key, string fileName, string contentType, TimeSpan expiresIn, CancellationToken cancellationToken) =>
        Task.FromResult($"http://storage.test/{key}?name={Uri.EscapeDataString(fileName)}&type={Uri.EscapeDataString(contentType)}");

    public Task<long?> GetSizeAsync(string key, CancellationToken cancellationToken) =>
        Task.FromResult(_objects.TryGetValue(key, out var bytes) ? bytes.LongLength : (long?)null);

    public Task<byte[]> ReadStartAsync(string key, int count, CancellationToken cancellationToken) =>
        Task.FromResult(_objects[key].Take(count).ToArray());

    public Task DeleteAsync(string key, CancellationToken cancellationToken)
    {
        _objects.TryRemove(key, out _);
        return Task.CompletedTask;
    }
}
