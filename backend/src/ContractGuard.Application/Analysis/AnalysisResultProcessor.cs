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

        var trace = new AnalysisTrace(document.SchemaVersion, document.Trace.ModelVersion, document.Trace.PromptVersion, RuleVersion: null);
        run.Complete(trace, clauses, now);
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
}
