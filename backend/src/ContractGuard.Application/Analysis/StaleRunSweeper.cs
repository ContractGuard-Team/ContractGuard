using ContractGuard.Application.Abstractions;
using ContractGuard.Domain.Analysis;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ContractGuard.Application.Analysis;

// SD-13: a running job that has been silent for too long is marked failed, so the user is not left
// waiting forever when the worker crashed or the message was lost.
// A job no worker ever answered fails sooner: the AI machines are stopped whenever the web is not in
// use, so a web that runs without them must say so rather than wait 20 minutes.
public sealed partial class StaleRunSweeper(IApplicationDbContext db, TimeProvider clock, ILogger<StaleRunSweeper> logger)
{
    public static readonly TimeSpan SilenceLimit = TimeSpan.FromMinutes(20);

    // Above the p95 of one analysis (5 minutes, DEC-GEN-022), so a job queued behind another is not failed.
    public static readonly TimeSpan PickupLimit = TimeSpan.FromMinutes(10);

    public async Task<int> SweepAsync(CancellationToken cancellationToken)
    {
        var now = clock.GetUtcNow();
        var silentSince = now - SilenceLimit;
        var unpickedSince = now - PickupLimit;

        // Every worker event moves LastEventAt, so LastEventAt == StartedAt means the worker never answered.
        var staleRuns = await db.AnalysisRuns
            .Include(r => r.Clauses)
            .Where(r => r.RunStatus == RunStatus.Running
                && (r.LastEventAt < silentSince || (r.LastEventAt == r.StartedAt && r.StartedAt < unpickedSince)))
            .ToListAsync(cancellationToken);

        foreach (var run in staleRuns)
        {
            var reason = run.LastEventAt == run.StartedAt
                ? $"The analysis worker did not pick up the job within {PickupLimit.TotalMinutes:0} minutes."
                : $"No answer from the analysis worker for {SilenceLimit.TotalMinutes:0} minutes.";
            run.Fail(reason, now);
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
