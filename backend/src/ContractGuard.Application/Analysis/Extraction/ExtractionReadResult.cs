namespace ContractGuard.Application.Analysis.Extraction;

// Result of IExtractionReader: either a schema-valid document, or the list of what is wrong.
public sealed record ExtractionReadResult(ExtractionDocument? Document, IReadOnlyList<string> Errors)
{
    public bool IsValid => Document is not null && Errors.Count == 0;

    public static ExtractionReadResult Valid(ExtractionDocument document) => new(document, []);

    public static ExtractionReadResult Invalid(IReadOnlyList<string> errors) => new(null, errors);
}
