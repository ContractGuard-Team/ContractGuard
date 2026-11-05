namespace ContractGuard.Application.Abstractions;

// Thrown by IObjectStorage.OpenReadAsync when the key does not exist.
public sealed class StoredObjectNotFoundException(string key)
    : Exception($"Object '{key}' was not found in storage.")
{
    public string Key { get; } = key;
}
