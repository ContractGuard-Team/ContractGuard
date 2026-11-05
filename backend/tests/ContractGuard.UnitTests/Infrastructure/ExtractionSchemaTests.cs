using System.Text.Json;
using System.Text.Json.Nodes;
using ContractGuard.Infrastructure.JsonSchemas;

namespace ContractGuard.UnitTests.Infrastructure;

// Checks contracts/extraction.v2.schema.json (embedded in Infrastructure) against the example
// and against broken variants, and that error messages stay readable.
public sealed class ExtractionSchemaTests
{
    [Fact]
    public void Example_is_valid()
    {
        var errors = Validate(TestData.ExampleExtractionJson(Guid.CreateVersion7(), Guid.CreateVersion7()));

        Assert.Empty(errors);
    }

    [Fact]
    public void Wrong_type_reports_only_the_real_problem()
    {
        var json = Node();
        json["clauses"] = "not-an-array";

        var errors = Validate(json.ToJsonString());

        var error = Assert.Single(errors);
        Assert.Equal("/clauses: Value is \"string\" but should be \"array\"", error);
    }

    [Fact]
    public void Problematic_finding_without_evidence_is_invalid()
    {
        // BR-RISK-013: no evidence, no finding.
        var json = Node();
        json["findings"]![0]!["evidence"] = new JsonArray();

        var errors = Validate(json.ToJsonString());

        Assert.Contains(errors, e => e.StartsWith("/findings/0/evidence", StringComparison.Ordinal));
    }

    [Fact]
    public void Unknown_property_is_invalid()
    {
        var json = Node();
        json["score"] = 42;

        Assert.Contains(Validate(json.ToJsonString()), e => e.StartsWith("/score", StringComparison.Ordinal));
    }

    [Fact]
    public void Wrong_date_format_is_invalid()
    {
        var json = Node();
        json["metadata"]!["effective_date"] = "01/01/2026";

        Assert.Contains(Validate(json.ToJsonString()), e => e.StartsWith("/metadata/effective_date", StringComparison.Ordinal));
    }

    private static JsonNode Node() =>
        JsonNode.Parse(TestData.ExampleExtractionJson(Guid.CreateVersion7(), Guid.CreateVersion7()))!;

    private static IReadOnlyList<string> Validate(string json)
    {
        using var document = JsonDocument.Parse(json);
        return ExtractionReader.Validate(document.RootElement);
    }
}
