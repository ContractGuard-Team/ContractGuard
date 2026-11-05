using System.Text.Json;
using ContractGuard.Application.Abstractions;
using ContractGuard.Application.Analysis.Extraction;
using ContractGuard.Infrastructure.Messaging;
using Json.Schema;

namespace ContractGuard.Infrastructure.JsonSchemas;

// Reads results/{job_id}/extraction.json from storage, validates it against the embedded
// extraction.v2 schema, then deserializes it. Invalid JSON never reaches the domain.
internal sealed class ExtractionReader(IObjectStorage storage) : IExtractionReader
{
    // A worker result is ~200 KB (system design §2.5). Anything far bigger is refused before parsing.
    private const long MaxBytes = 20L * 1024 * 1024;

    private static readonly Lazy<JsonSchema> Schema = new(LoadSchema);

    private static readonly EvaluationOptions Evaluation = new()
    {
        OutputFormat = OutputFormat.Hierarchical,
        RequireFormatValidation = true
    };

    public async Task<ExtractionReadResult> ReadAsync(string key, CancellationToken cancellationToken)
    {
        await using var stream = await storage.OpenReadAsync(key, cancellationToken);
        using var buffer = new MemoryStream();
        await stream.CopyToAsync(buffer, cancellationToken);
        if (buffer.Length > MaxBytes)
        {
            return ExtractionReadResult.Invalid([$"Result is larger than {MaxBytes} bytes."]);
        }

        buffer.Position = 0;
        JsonDocument json;
        try
        {
            json = await JsonDocument.ParseAsync(buffer, cancellationToken: cancellationToken);
        }
        catch (JsonException ex)
        {
            return ExtractionReadResult.Invalid([$"Result is not valid JSON: {ex.Message}"]);
        }

        using (json)
        {
            var errors = Validate(json.RootElement);
            if (errors.Count > 0)
            {
                return ExtractionReadResult.Invalid(errors);
            }

            var document = json.RootElement.Deserialize<ExtractionDocument>(MessageJson.Options)
                ?? throw new InvalidOperationException("Schema-valid result deserialized to null.");
            return ExtractionReadResult.Valid(document);
        }
    }

    // Exposed for tests: validate any JSON element against the extraction schema.
    internal static IReadOnlyList<string> Validate(JsonElement root)
    {
        var result = Schema.Value.Evaluate(root, Evaluation);
        if (result.IsValid)
        {
            return [];
        }

        var errors = new List<string>();
        CollectErrors(result, errors);
        return errors.Distinct().Take(50).ToList();
    }

    // Walks the result tree and keeps only errors that explain the failure. A subtree that is
    // valid as a whole is skipped, even if branches inside it failed: a failed "anyOf" branch or
    // "if" condition is expected and would only confuse whoever reads the log.
    private static void CollectErrors(EvaluationResults node, List<string> errors)
    {
        if (node.IsValid || node.EvaluationPath.ToString().EndsWith("/if", StringComparison.Ordinal))
        {
            return;
        }

        var location = node.InstanceLocation.ToString() is { Length: > 0 } path ? path : "(root)";
        foreach (var (keyword, message) in node.Errors ?? new Dictionary<string, string>())
        {
            // Applicator keywords only summarize their children ("Some properties did not match").
            if (keyword is not ("properties" or "items" or "allOf" or "additionalProperties" or "$ref"))
            {
                errors.Add($"{location}: {message}");
            }
        }

        foreach (var child in node.Details ?? [])
        {
            CollectErrors(child, errors);
        }
    }

    private static JsonSchema LoadSchema()
    {
        using var stream = typeof(ExtractionReader).Assembly.GetManifestResourceStream("extraction.v2.schema.json")
            ?? throw new InvalidOperationException("Embedded extraction.v2.schema.json is missing.");
        using var reader = new StreamReader(stream);
        return JsonSchema.FromText(reader.ReadToEnd());
    }
}
