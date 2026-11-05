using ContractGuard.Application.Abstractions;
using ContractGuard.Domain.Analysis;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ContractGuard.Application.Analysis;

// SD-13: a running job that has been silent for too long is marked failed, so the user is not left
// waiting forever when the worker crashed or the message was lost.
public sealed partial class StaleRunSweeper(IApplicationDbContext db, TimeProvider clock, ILogger<StaleRunSweeper> logger)
{
    public static readonly TimeSpan SilenceLimit = TimeSpan.FromMinutes(20);

    public async Task<int> SweepAsync(CancellationToken cancellationToken)
    {
        var now = clock.GetUtcNow();
        var cutoff = now - SilenceLimit;
        var staleRuns = await db.AnalysisRuns
            .Include(r => r.Clauses)
            .Where(r => r.RunStatus == RunStatus.Running && r.LastEventAt < cutoff)
            .ToListAsync(cancellationToken);

        foreach (var run in staleRuns)
        {
            run.Fail($"No answer from the analysis worker for {SilenceLimit.TotalMinutes:0} minutes.", now);
            var contract = await db.Contracts.SingleAsync(c => c.ContractId == run.ContractId, cancellationToken);
            contract.MarkAnalysisFailed(now);
            LogStale(run.AnalysisRunId);
        }

        if (staleRuns.Count > 0)
        {
            await db.SaveChangesAsync(cancellationToken);
        }

        return staleRuns.Count;
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Job {JobId} timed out and was marked failed")]
    private partial void LogStale(Guid jobId);
}
