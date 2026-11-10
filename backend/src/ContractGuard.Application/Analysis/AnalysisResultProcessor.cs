using ContractGuard.Application.Abstractions;
using ContractGuard.Application.Analysis.Extraction;
using ContractGuard.Domain.Analysis;
using ContractGuard.Domain.Contracts;
using Microsoft.EntityFrameworkCore;

namespace ContractGuard.Application.Analysis;

// Turns a validated extraction document into domain state. Does not save: the handler saves once,
// so either everything of the result is stored or nothing is (FR-ANA-005).
public sealed class AnalysisResultProcessor(IApplicationDbContext db, TimeProvider clock)
{
    public async Task<ResultProcessingOutcome> ApplyAsync(
        AnalysisRun run,
        Contract contract,
        ContractVersion version,
        ExtractionDocument document,
        CancellationToken cancellationToken)
    {
        var now = clock.GetUtcNow();

        version.RecordDocumentInfo(document.Document.PageCount, document.Document.CleanFileKey);

        var clauses = document.Clauses.Select(c => new ClauseData(
            c.ClauseId,
            c.Order,
            c.Number,
            c.Heading,
            c.Text,
            c.ClauseType,
            new ClauseLocation(
                c.Location.PageStart,
                c.Location.PageEnd,
                [.. c.Location.Regions.Select(r => new PageRegion(r.Page, r.X, r.Y, r.Width, r.Height))])));

        version.RecordExtractedMetadata(ToMetadata(document.Metadata));

        var trace = new AnalysisTrace(document.SchemaVersion, document.Trace.ModelVersion, document.Trace.PromptVersion, document.Scoring?.RuleVersion);
        run.Complete(trace, clauses, ToResults(document), now);
        contract.MarkAnalysisCompleted(now);

        // FR-ANA-015: a successful re-run replaces the previous results of the same version.
        var previousRuns = await db.AnalysisRuns
            .Where(r => r.ContractVersionId == run.ContractVersionId
                && r.AnalysisRunId != run.AnalysisRunId
                && r.RunStatus == RunStatus.Completed)
            .ToListAsync(cancellationToken);
        db.AnalysisRuns.RemoveRange(previousRuns);

        return ResultProcessingOutcome.Success;
    }

    private static AnalysisResults ToResults(ExtractionDocument document) => new(
        document.Findings.Select(f => new FindingData(
            f.FindingId,
            Parse<FindingKind>(f.FindingKind),
            f.Title,
            Parse<RiskDomain>(f.PrimaryDomain),
            [.. (f.SecondaryDomains ?? []).Select(Parse<RiskDomain>)],
            f.Category,
            Parse<Severity>(f.Severity),
            f.ClauseId,
            [.. (f.Evidence ?? []).Select(e => new Evidence(e.ClauseId, e.Page, e.Quote))],
            f.MissingDescription,
            f.Why,
            f.Impact,
            f.Recommendation,
            [.. (f.References ?? []).Select(r => new FindingReference(r.Source, r.ReferenceNumber))],
            f.CheckCode)),
        [.. document.Checks
            .OrderBy(c => c.CheckCode, StringComparer.Ordinal)
            .Select(c => new CheckResult(c.CheckCode, Parse<CheckStatus>(c.CheckStatus), [.. c.ClauseIds ?? []], c.FindingId, c.Note))],
        document.Scoring is { } scoring
            ? new RiskScore(
                scoring.OverallScore,
                Parse<RiskLevel>(scoring.RiskLevel),
                [.. scoring.Domains
                    .Select(d => new DomainScore(
                        Parse<RiskDomain>(d.Domain),
                        d.Applicable,
                        d.Score,
                        d.RiskLevel is null ? null : Parse<RiskLevel>(d.RiskLevel),
                        d.NaReason,
                        d.ReEnabled ?? false))
                    .OrderBy(d => d.Domain)])
            : null,
        [.. (document.InjectionWarnings ?? []).Select(w => new InjectionWarning(
            w.Page,
            w.Region is { } r ? new PageRegion(r.Page, r.X, r.Y, r.Width, r.Height) : null,
            w.Excerpt))],
        document.OverallRecommendation);

    private static ContractMetadata? ToMetadata(ExtractedMetadata? m) => m is null
        ? null
        : new ContractMetadata(
            [.. m.Parties ?? []],
            m.ContractType is null ? null : Parse<ContractType>(m.ContractType),
            m.ContractValue,
            m.Currency,
            m.EffectiveDate,
            m.ExpiryDate,
            m.GoverningLaw);

    // Values were checked against the schema enums, so "data_privacy" always maps to DataPrivacy.
    private static T Parse<T>(string snakeCase)
        where T : struct, Enum => Enum.Parse<T>(snakeCase.Replace("_", string.Empty, StringComparison.Ordinal), ignoreCase: true);
}
