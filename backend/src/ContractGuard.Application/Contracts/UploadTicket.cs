namespace ContractGuard.Application.Contracts;

// What the browser needs to upload a file: send it with CreateContract afterwards as UploadId.
public sealed record UploadTicket(Guid UploadId, string Url, IReadOnlyDictionary<string, string> Fields);
