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

    [Fact]
    public void Worker_levels_are_kept_even_when_they_differ_from_the_srs_thresholds()
    {
        // docs/04 P7-01, real worker output 2026-11-20: policy 4.5b-1.2.2 calls 5 points "medium" (SRS: low).
        var document = WithScoring(s => s with
        {
            RiskLevel = "medium",
            Domains = [.. s.Domains.Select(d => d.Domain == "operational" ? d with { Score = 5, RiskLevel = "medium" } : d)]
        });

        Assert.Empty(Validate(document));
        Assert.Empty(ExtractionConsistencyValidator.ScoringProblems(document));
    }

    [Fact]
    public void Critical_finding_below_the_floor_is_only_a_warning()
    {
        var document = WithScoring(s => s with { OverallScore = 40, RiskLevel = "medium" });

        Assert.Empty(Validate(document));
        Assert.Empty(ExtractionConsistencyValidator.ScoringProblems(document));
        Assert.Contains(ExtractionConsistencyValidator.ScoringWarnings(document), e => e.Contains("FR-RISK-022", StringComparison.Ordinal));
    }

    [Fact]
    public void Na_domain_with_a_score_or_without_reason_drops_the_scoring()
    {
        var document = WithScoring(s => s with
        {
            Domains = [.. s.Domains.Select(d => d.Domain == "compliance" ? d with { Score = 10, NaReason = null } : d)]
        });

        Assert.Empty(Validate(document));
        Assert.Contains(ExtractionConsistencyValidator.ScoringProblems(document), e => e.Contains("FR-VIEW-003", StringComparison.Ordinal));
    }

    [Fact]
    public void Applicable_domain_without_a_score_drops_the_scoring()
    {
        var document = WithScoring(s => s with
        {
            Domains = [.. s.Domains.Select(d => d.Domain == "legal" ? d with { Score = null } : d)]
        });

        Assert.Contains(ExtractionConsistencyValidator.ScoringProblems(document), e => e.Contains("FR-RISK-019", StringComparison.Ordinal));
    }

    [Fact]
    public void Finding_in_a_na_domain_is_only_a_warning()
    {
        // The worker marks cybersecurity N/A (no rule in its policy) yet CY checks yield cybersecurity findings.
        var document = WithScoring(s => s with
        {
            Domains = [.. s.Domains.Select(d => d.Domain == "third_party" ? d with { Applicable = false, Score = null, RiskLevel = null, NaReason = "x" } : d)]
        });

        Assert.Empty(Validate(document));
        Assert.Empty(ExtractionConsistencyValidator.ScoringProblems(document));
        Assert.Contains(ExtractionConsistencyValidator.ScoringWarnings(document), e => e.Contains("BR-RISK-003", StringComparison.Ordinal));
    }

    [Fact]
    public void Missing_domain_drops_the_scoring()
    {
        var document = WithScoring(s => s with { Domains = [.. s.Domains.Take(6), s.Domains[0]] });

        Assert.Empty(Validate(document));
        Assert.Contains(ExtractionConsistencyValidator.ScoringProblems(document), e => e.Contains("REL-DATA-014", StringComparison.Ordinal));
    }

    [Fact]
    public void Finding_from_a_check_takes_the_check_domain()
    {
        var document = TestData.ExampleExtraction(JobId, ContractId);
        document = document with { Findings = [document.Findings[0] with { PrimaryDomain = "legal" }, .. document.Findings.Skip(1)] };

        Assert.Contains(Validate(document), e => e.Contains("BR-RISK-002", StringComparison.Ordinal));
    }

    [Fact]
    public void Result_without_scoring_is_still_accepted()
    {
        // Scoring is optional until the AI team agrees on it (docs/06-ai-integration-points.md).
        var document = TestData.ExampleExtraction(JobId, ContractId) with { Scoring = null };

        Assert.Empty(Validate(document));
    }

    private static IReadOnlyList<string> Validate(ExtractionDocument document) =>
        ExtractionConsistencyValidator.Validate(document, JobId, ContractId, 1);

    private static ExtractionDocument WithScoring(Func<ExtractedScoring, ExtractedScoring> change)
    {
        var document = TestData.ExampleExtraction(JobId, ContractId);
        return document with { Scoring = change(document.Scoring!) };
    }
}
