using System.Text.RegularExpressions;

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

        foreach (var item in document.Embeddings?.Items ?? [])
        {
            if (!clauses.ContainsKey(item.ClauseId))
            {
                errors.Add($"Embedding refers to unknown clause '{item.ClauseId}'.");
            }
        }

        return errors;
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
