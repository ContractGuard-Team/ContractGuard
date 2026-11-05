using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;
using ContractGuard.Application.Abstractions;

namespace ContractGuard.IntegrationTests;

// Stand-in for S3 in tests: a dictionary of key -> bytes.
public sealed class InMemoryObjectStorage : IObjectStorage
{
    private readonly ConcurrentDictionary<string, byte[]> _objects = new();

    public void PutText(string key, string text) => _objects[key] = Encoding.UTF8.GetBytes(text);

    public bool Exists(string key) => _objects.ContainsKey(key);

    public string GetText(string key) => Encoding.UTF8.GetString(_objects[key]);

    public Task PutJsonAsync<T>(string key, T value, CancellationToken cancellationToken)
    {
        _objects[key] = JsonSerializer.SerializeToUtf8Bytes(value);
        return Task.CompletedTask;
    }

    public Task<Stream> OpenReadAsync(string key, CancellationToken cancellationToken) =>
        _objects.TryGetValue(key, out var bytes)
            ? Task.FromResult<Stream>(new MemoryStream(bytes, writable: false))
            : throw new StoredObjectNotFoundException(key);
}
