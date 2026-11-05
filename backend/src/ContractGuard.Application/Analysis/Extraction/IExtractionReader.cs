namespace ContractGuard.Application.Analysis.Extraction;

// Reads a worker result from storage and validates it against extraction.v2.schema.json.
public interface IExtractionReader
{
    Task<ExtractionReadResult> ReadAsync(string key, CancellationToken cancellationToken);
}
