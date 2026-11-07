namespace ContractGuard.Application.Abstractions;

// A signed HTML form: the browser POSTs these fields plus the file to this URL.
public sealed record PresignedUpload(string Url, IReadOnlyDictionary<string, string> Fields);
