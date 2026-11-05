namespace ContractGuard.Application.Analysis.Extraction;

// C# view of contracts/extraction.v2.schema.json. Property names map to snake_case JSON keys.
// Only built after the JSON has passed schema validation, so required fields are non-null.
// Exception to "one type per file": every record of the schema stays in this file, in the order
// of the JSON, so the whole document shape reads in one place, like the schema itself.
public sealed record ExtractionDocument(
    string SchemaVersion,
    string JobId,
    string? TenantId,
    string ContractId,
    string ContractVersion,
    bool DocumentComplete,
    ExtractedDocumentInfo Document,
    string? OutputLanguage,
    IReadOnlyList<ExtractedClause> Clauses,
    ExtractedMetadata? Metadata,
    IReadOnlyList<ExtractedFinding> Findings,
    IReadOnlyList<ExtractedCheck> Checks,
    IReadOnlyDictionary<string, ExtractedFact> Facts,
    IReadOnlyList<ExtractedInjectionWarning>? InjectionWarnings,
    string? OverallRecommendation,
    ExtractedTrace Trace,
    ExtractedEmbeddings? Embeddings);

// "document" of extraction.v2: facts about the file as the worker read it.
public sealed record ExtractedDocumentInfo(string FileType, int PageCount, string? CleanFileKey, string Language, bool? IsScanned);

// A rectangle on a page, as fractions (0..1) of the page size.
public sealed record ExtractedRegion(int Page, double X, double Y, double Width, double Height);

// "location" of a clause: pages and rectangles where it sits in the file.
public sealed record ExtractedLocation(int PageStart, int PageEnd, IReadOnlyList<ExtractedRegion> Regions);

// One item of "clauses" in extraction.v2.
public sealed record ExtractedClause(
    string ClauseId,
    int Order,
    string Number,
    string? Heading,
    string Text,
    string ClauseType,
    ExtractedLocation Location);

// "metadata" of extraction.v2: parties, value, dates, governing law.
public sealed record ExtractedMetadata(
    IReadOnlyList<string>? Parties,
    string? ContractType,
    decimal? ContractValue,
    string? Currency,
    DateOnly? EffectiveDate,
    DateOnly? ExpiryDate,
    string? GoverningLaw);

// A verbatim quote from a clause, used as evidence by findings and facts.
public sealed record ExtractedEvidence(string ClauseId, int Page, string Quote);

// A standard or law a finding refers to (e.g. NIST SP 800-161r1, SR-3).
public sealed record ExtractedReference(string Source, string ReferenceNumber);

// One item of "findings" in extraction.v2: a risk found in a clause, or a missing clause.
public sealed record ExtractedFinding(
    string FindingId,
    string FindingKind,
    string Title,
    string PrimaryDomain,
    IReadOnlyList<string>? SecondaryDomains,
    string? Category,
    string Severity,
    string? ClauseId,
    IReadOnlyList<ExtractedEvidence>? Evidence,
    string? MissingDescription,
    string Why,
    string Impact,
    string Recommendation,
    IReadOnlyList<ExtractedReference>? References,
    string? CheckCode);

// One item of "checks" in extraction.v2: the status of one of the 20 check codes.
public sealed record ExtractedCheck(
    string CheckCode,
    string CheckStatus,
    IReadOnlyList<string>? ClauseIds,
    string? FindingId,
    string? Note);

// One value of "facts" in extraction.v2 (e.g. incident.notification_hours) with its evidence.
public sealed record ExtractedFact(string Status, System.Text.Json.JsonElement Value, IReadOnlyList<ExtractedEvidence> Evidence);

// One item of "injection_warnings": text in the file that tried to give the AI instructions.
public sealed record ExtractedInjectionWarning(int Page, ExtractedRegion? Region, string Excerpt);

// "trace" of extraction.v2: which model and prompts produced the result.
public sealed record ExtractedTrace(string ModelVersion, string PromptVersion, string? PipelineVersion);

// The embedding vector of one clause.
public sealed record ExtractedEmbeddingItem(string ClauseId, IReadOnlyList<float> Vector);

// "embeddings" of extraction.v2 (optional): clause vectors for the chatbot (Phase 4).
public sealed record ExtractedEmbeddings(string Model, int Dimensions, IReadOnlyList<ExtractedEmbeddingItem> Items);
