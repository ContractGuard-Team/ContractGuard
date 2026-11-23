using System.Text.RegularExpressions;
using ContractGuard.Domain.Analysis;

namespace ContractGuard.Application.Analysis.Extraction;

// Trust boundary (docs/architecture/data-contract.md, step 4). The JSON Schema only checks the
// shape. This checks that the content is consistent with what the backend knows and with itself:
// ids match the job, every reference points to an existing clause, quotes really appear in the
// clause text, page numbers exist. Output of an LLM is never trusted on these points.
public static partial class ExtractionConsistencyValidator
{
    public static readonly IReadOnlySet<string> AllCheckCodes = new HashSet<string>(
        Enumerable.Range(1, 12).Select(i => $"CY-{i:00}")
            .Concat(Enumerable.Range(1, 5).Select(i => $"PR-{i:00}"))
            .Concat(Enumerable.Range(1, 3).Select(i => $"TP-{i:00}")));

    // BR-RISK-002 rule 1: a finding produced by a check takes the domain of the check's prefix.
    private static readonly Dictionary<string, string> CheckPrefixDomains = new(StringComparer.Ordinal)
    {
        ["CY"] = "cybersecurity",
        ["PR"] = "data_privacy",
        ["TP"] = "third_party"
    };

    public static IReadOnlyList<string> Validate(
        ExtractionDocument document,
        Guid jobId,
        Guid contractId,
        int versionNumber)
    {
        var errors = new List<string>();

        if (!Guid.TryParse(document.JobId, out var docJobId) || docJobId != jobId)
        {
            errors.Add($"job_id '{document.JobId}' does not match the job {jobId}.");
        }

        if (!Guid.TryParse(document.ContractId, out var docContractId) || docContractId != contractId)
        {
            errors.Add($"contract_id '{document.ContractId}' does not match the job's contract.");
        }

        if (document.ContractVersion != versionNumber.ToString(System.Globalization.CultureInfo.InvariantCulture))
        {
            errors.Add($"contract_version '{document.ContractVersion}' does not match version {versionNumber}.");
        }

        var pageCount = document.Document.PageCount;
        var clauses = new Dictionary<string, ExtractedClause>(StringComparer.Ordinal);
        foreach (var clause in document.Clauses)
        {
            if (!clauses.TryAdd(clause.ClauseId, clause))
            {
                errors.Add($"Duplicate clause_id '{clause.ClauseId}'.");
            }

            var location = clause.Location;
            if (location.PageStart > location.PageEnd || location.PageEnd > pageCount
                || location.Regions.Any(r => r.Page < location.PageStart || r.Page > location.PageEnd))
            {
                errors.Add($"Clause '{clause.ClauseId}' has a location outside pages 1..{pageCount}.");
            }
        }

        if (document.Clauses.Select(c => c.Order).Distinct().Count() != document.Clauses.Count)
        {
            errors.Add("Clause order values are not unique.");
        }

        var findingIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (var finding in document.Findings)
        {
            if (!findingIds.Add(finding.FindingId))
            {
                errors.Add($"Duplicate finding_id '{finding.FindingId}'.");
            }

            if (finding.ClauseId is not null && !clauses.ContainsKey(finding.ClauseId))
            {
                errors.Add($"Finding '{finding.FindingId}' cites unknown clause '{finding.ClauseId}'.");
            }

            CheckEvidence(finding.Evidence ?? [], clauses, pageCount, $"Finding '{finding.FindingId}'", errors);

            if (finding.CheckCode is not null && CheckPrefixDomains[finding.CheckCode[..2]] != finding.PrimaryDomain)
            {
                errors.Add($"Finding '{finding.FindingId}' comes from check {finding.CheckCode}, so its primary domain must be {CheckPrefixDomains[finding.CheckCode[..2]]} (BR-RISK-002).");
            }
        }

        var checkCodes = document.Checks.Select(c => c.CheckCode).ToList();
        if (checkCodes.Distinct().Count() != checkCodes.Count || !AllCheckCodes.SetEquals(checkCodes))
        {
            errors.Add("checks must contain each of the 20 check codes of BR-RISK-007 exactly once.");
        }

        foreach (var check in document.Checks)
        {
            if (check.FindingId is not null && !findingIds.Contains(check.FindingId))
            {
                errors.Add($"Check '{check.CheckCode}' refers to unknown finding '{check.FindingId}'.");
            }

            foreach (var clauseId in check.ClauseIds ?? [])
            {
                if (!clauses.ContainsKey(clauseId))
                {
                    errors.Add($"Check '{check.CheckCode}' refers to unknown clause '{clauseId}'.");
                }
            }
        }

        foreach (var (key, fact) in document.Facts)
        {
            CheckEvidence(fact.Evidence, clauses, pageCount, $"Fact '{key}'", errors);
        }

        foreach (var warning in document.InjectionWarnings ?? [])
        {
            if (warning.Page > pageCount)
            {
                errors.Add($"Injection warning on page {warning.Page} is outside the document.");
            }
        }

        return errors;
    }

    // docs/04 P7-01: the worker computes scores and levels with its own versioned policy (scoring.rule_version),
    // so the backend keeps its levels as they are and does not re-derive them from score thresholds.
    // Scoring never rejects a result: a block the web cannot show is dropped (these problems), the clauses
    // and findings are still saved.
    public static IReadOnlyList<string> ScoringProblems(ExtractionDocument document)
    {
        var problems = new List<string>();
        var scoring = document.Scoring;
        if (scoring is null)
        {
            return problems;
        }

        if (scoring.Domains.Select(d => d.Domain).Distinct().Count() != Enum.GetValues<RiskDomain>().Length
            || scoring.Domains.Count != Enum.GetValues<RiskDomain>().Length)
        {
            problems.Add("scoring.domains must list each of the 7 domains exactly once (REL-DATA-014).");
        }

        foreach (var domain in scoring.Domains)
        {
            if (!domain.Applicable)
            {
                if (domain.Score is not null || domain.RiskLevel is not null || string.IsNullOrWhiteSpace(domain.NaReason))
                {
                    problems.Add($"Domain '{domain.Domain}' is N/A, so it needs a reason and no score (FR-VIEW-003).");
                }
            }
            else if (domain.Score is null || domain.RiskLevel is null)
            {
                problems.Add($"Domain '{domain.Domain}' applies, so it needs a score and a level (FR-RISK-019).");
            }
        }

        return problems;
    }

    // Rules the worker's policy does not follow yet; logged for the AI team, the scoring is kept (docs/04 P7-01).
    public static IReadOnlyList<string> ScoringWarnings(ExtractionDocument document)
    {
        var warnings = new List<string>();
        var scoring = document.Scoring;
        if (scoring is null)
        {
            return warnings;
        }

        var notApplicable = scoring.Domains.Where(d => !d.Applicable).Select(d => d.Domain).ToHashSet(StringComparer.Ordinal);
        foreach (var finding in document.Findings.Where(f => notApplicable.Contains(f.PrimaryDomain)))
        {
            warnings.Add($"Finding '{finding.FindingId}' is in domain '{finding.PrimaryDomain}', which is N/A (BR-RISK-003).");
        }

        if (document.Findings.Any(f => f.Severity == "critical") && scoring.OverallScore < RiskLevels.CriticalFindingFloor)
        {
            warnings.Add($"Overall score {scoring.OverallScore} is below {RiskLevels.CriticalFindingFloor} although there is a critical finding (FR-RISK-022).");
        }

        return warnings;
    }

    private static void CheckEvidence(
        IReadOnlyList<ExtractedEvidence> evidence,
        Dictionary<string, ExtractedClause> clauses,
        int pageCount,
        string owner,
        List<string> errors)
    {
        foreach (var item in evidence)
        {
            if (!clauses.TryGetValue(item.ClauseId, out var clause))
            {
                errors.Add($"{owner} quotes unknown clause '{item.ClauseId}'.");
                continue;
            }

            if (item.Page < clause.Location.PageStart || item.Page > clause.Location.PageEnd || item.Page > pageCount)
            {
                errors.Add($"{owner} quotes clause '{item.ClauseId}' on page {item.Page}, which is not where the clause is.");
            }

            if (!Normalize(clause.Text).Contains(Normalize(item.Quote), StringComparison.OrdinalIgnoreCase))
            {
                errors.Add($"{owner} quote is not found verbatim in clause '{item.ClauseId}'.");
            }
        }
    }

    // Collapses whitespace and unifies quote characters so line breaks or typographic quotes
    // from PDF extraction do not cause false mismatches.
    private static string Normalize(string text) =>
        WhitespacePattern().Replace(text, " ").Replace('“', '"').Replace('”', '"').Replace('’', '\'').Trim();

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespacePattern();
}
