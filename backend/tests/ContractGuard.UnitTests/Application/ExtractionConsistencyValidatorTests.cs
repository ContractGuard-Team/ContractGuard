using ContractGuard.Application.Analysis.Extraction;

namespace ContractGuard.UnitTests.Application;

public sealed class ExtractionConsistencyValidatorTests
{
    private static readonly Guid JobId = Guid.CreateVersion7();
    private static readonly Guid ContractId = Guid.CreateVersion7();

    [Fact]
    public void Example_result_is_consistent()
    {
        var document = TestData.ExampleExtraction(JobId, ContractId);

        var errors = ExtractionConsistencyValidator.Validate(document, JobId, ContractId, versionNumber: 1);

        Assert.Empty(errors);
    }

    [Fact]
    public void Ids_from_another_job_are_rejected()
    {
        var document = TestData.ExampleExtraction(Guid.CreateVersion7(), Guid.CreateVersion7(), version: 2);

        var errors = ExtractionConsistencyValidator.Validate(document, JobId, ContractId, versionNumber: 1);

        Assert.Contains(errors, e => e.StartsWith("job_id", StringComparison.Ordinal));
        Assert.Contains(errors, e => e.StartsWith("contract_id", StringComparison.Ordinal));
        Assert.Contains(errors, e => e.StartsWith("contract_version", StringComparison.Ordinal));
    }

    [Fact]
    public void Invented_quote_is_rejected()
    {
        // The LLM claims the clause says something it does not say.
        var document = TestData.ExampleExtraction(JobId, ContractId);
        var finding = document.Findings[0];
        var invented = finding with { Evidence = [new ExtractedEvidence(finding.ClauseId!, 2, "Provider accepts unlimited liability.")] };
        document = document with { Findings = [invented, .. document.Findings.Skip(1)] };

        var errors = ExtractionConsistencyValidator.Validate(document, JobId, ContractId, 1);

        Assert.Contains(errors, e => e.Contains("not found verbatim", StringComparison.Ordinal));
    }

    [Fact]
    public void Quote_with_different_line_breaks_is_accepted()
    {
        var document = TestData.ExampleExtraction(JobId, ContractId);
        var finding = document.Findings[0];
        var quote = "In no event shall Provider\n be liable   for any loss of data";
        document = document with { Findings = [finding with { Evidence = [new ExtractedEvidence("cl-006", 2, quote)] }, .. document.Findings.Skip(1)] };

        Assert.Empty(ExtractionConsistencyValidator.Validate(document, JobId, ContractId, 1));
    }

    [Fact]
    public void Reference_to_unknown_clause_is_rejected()
    {
        var document = TestData.ExampleExtraction(JobId, ContractId);
        document = document with { Findings = [document.Findings[0] with { ClauseId = "cl-999" }, .. document.Findings.Skip(1)] };

        var errors = ExtractionConsistencyValidator.Validate(document, JobId, ContractId, 1);

        Assert.Contains(errors, e => e.Contains("unknown clause 'cl-999'", StringComparison.Ordinal));
    }

    [Fact]
    public void Duplicated_check_code_is_rejected()
    {
        var document = TestData.ExampleExtraction(JobId, ContractId);
        var checks = document.Checks.ToList();
        checks[1] = checks[1] with { CheckCode = checks[0].CheckCode };
        document = document with { Checks = checks };

        var errors = ExtractionConsistencyValidator.Validate(document, JobId, ContractId, 1);

        Assert.Contains(errors, e => e.StartsWith("checks must contain", StringComparison.Ordinal));
    }

    [Fact]
    public void Clause_on_a_page_beyond_the_document_is_rejected()
    {
        var document = TestData.ExampleExtraction(JobId, ContractId);
        document = document with { Document = document.Document with { PageCount = 2 } };

        var errors = ExtractionConsistencyValidator.Validate(document, JobId, ContractId, 1);

        Assert.Contains(errors, e => e.Contains("outside pages 1..2", StringComparison.Ordinal));
    }
}
