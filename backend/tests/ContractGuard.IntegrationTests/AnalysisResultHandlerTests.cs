using System.Text.RegularExpressions;
using ContractGuard.Application.Analysis;
using ContractGuard.Domain.Analysis;
using ContractGuard.Domain.Contracts;
using ContractGuard.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;

namespace ContractGuard.IntegrationTests;

// The results pipeline against a real PostgreSQL: handler -> reader (schema) -> validator -> processor -> DB.
[Collection(ApiCollection.Name)]
public sealed class AnalysisResultHandlerTests(ApiFactory factory)
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Completed_result_is_saved_and_contract_moves_to_pending_review()
    {
        var (contract, run) = await SeedAsync();
        var key = PutExampleResult(run.AnalysisRunId, contract.ContractId);

        var outcome = await HandleAsync(Completed(run.AnalysisRunId, key));

        Assert.Equal(ResultOutcome.Applied, outcome);
        await using var scope = factory.CreateSystemScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var savedRun = await db.AnalysisRuns.Include(r => r.Clauses).SingleAsync(r => r.AnalysisRunId == run.AnalysisRunId, Ct);
        Assert.Equal(RunStatus.Completed, savedRun.RunStatus);
        Assert.Equal(8, savedRun.Clauses.Count);
        Assert.Equal("2.0.0", savedRun.SchemaVersion);
        var location = savedRun.Clauses.Single(c => c.ClauseKey == "cl-007").Location;
        Assert.Equal(2, location.Regions.Count);
        var savedContract = await db.Contracts.Include(c => c.Versions).SingleAsync(c => c.ContractId == contract.ContractId, Ct);
        Assert.Equal(ApprovalStatus.PendingReview, savedContract.ApprovalStatus);
        Assert.Equal(3, savedContract.LatestVersion.PageCount);
    }

    [Fact]
    public async Task Duplicate_completed_message_is_ignored()
    {
        var (contract, run) = await SeedAsync();
        var key = PutExampleResult(run.AnalysisRunId, contract.ContractId);

        await HandleAsync(Completed(run.AnalysisRunId, key));
        var second = await HandleAsync(Completed(run.AnalysisRunId, key));

        Assert.Equal(ResultOutcome.Ignored, second);
        Assert.Equal(8, await CountClausesAsync(run.AnalysisRunId));
    }

    [Fact]
    public async Task Worker_scoring_levels_are_saved_as_written()
    {
        // docs/04 P7-01: the worker's policy calls 5 points "medium"; the backend no longer rejects that.
        var (contract, run) = await SeedAsync();
        var key = $"results/{run.AnalysisRunId}/extraction.json";
        var json = Regex.Replace(Seed.ExampleExtractionJson(run.AnalysisRunId, contract.ContractId),
                "(\"overall_score\": 72,\\s*\"risk_level\": )\"high\"", "$1\"medium\"")
            .Replace("\"domain\": \"operational\", \"applicable\": true, \"score\": 15, \"risk_level\": \"low\"",
                     "\"domain\": \"operational\", \"applicable\": true, \"score\": 5, \"risk_level\": \"medium\"", StringComparison.Ordinal);
        Assert.Contains("\"score\": 5, \"risk_level\": \"medium\"", json, StringComparison.Ordinal);
        Assert.Matches("\"overall_score\": 72,\\s*\"risk_level\": \"medium\"", json);
        factory.Storage.PutText(key, json);

        var outcome = await HandleAsync(Completed(run.AnalysisRunId, key));

        Assert.Equal(ResultOutcome.Applied, outcome);
        await using var scope = factory.CreateSystemScope();
        var saved = await scope.ServiceProvider.GetRequiredService<AppDbContext>().AnalysisRuns
            .SingleAsync(r => r.AnalysisRunId == run.AnalysisRunId, Ct);
        Assert.Equal(RiskLevel.Medium, saved.OverallRiskLevel);
        Assert.Equal(RiskLevel.Medium, saved.Score!.Domains.Single(d => d.Domain == RiskDomain.Operational).RiskLevel);
    }

    [Fact]
    public async Task Broken_scoring_is_dropped_and_the_rest_of_the_result_is_saved()
    {
        var (contract, run) = await SeedAsync();
        var key = $"results/{run.AnalysisRunId}/extraction.json";
        var json = Seed.ExampleExtractionJson(run.AnalysisRunId, contract.ContractId)
            .Replace("\"domain\": \"legal\", \"applicable\": true, \"score\": 20,", "\"domain\": \"legal\", \"applicable\": true, \"score\": null,", StringComparison.Ordinal);
        Assert.Contains("\"domain\": \"legal\", \"applicable\": true, \"score\": null,", json, StringComparison.Ordinal);
        factory.Storage.PutText(key, json);

        var outcome = await HandleAsync(Completed(run.AnalysisRunId, key));

        Assert.Equal(ResultOutcome.Applied, outcome);
        Assert.Equal(8, await CountClausesAsync(run.AnalysisRunId));
        await using var scope = factory.CreateSystemScope();
        var saved = await scope.ServiceProvider.GetRequiredService<AppDbContext>().AnalysisRuns
            .SingleAsync(r => r.AnalysisRunId == run.AnalysisRunId, Ct);
        Assert.Equal(RunStatus.Completed, saved.RunStatus);
        Assert.Null(saved.Score);
    }

    [Fact]
    public async Task Result_violating_the_schema_fails_the_run_and_keeps_nothing()
    {
        var (contract, run) = await SeedAsync();
        var key = $"results/{run.AnalysisRunId}/extraction.json";
        factory.Storage.PutText(key, Seed.ExampleExtractionJson(run.AnalysisRunId, contract.ContractId)
            .Replace("\"clauses\": [", "\"clauses\": \"oops\", \"unused\": [", StringComparison.Ordinal));

        var outcome = await HandleAsync(Completed(run.AnalysisRunId, key));

        Assert.Equal(ResultOutcome.Rejected, outcome);
        await AssertFailedAsync(run.AnalysisRunId, contract.ContractId, "invalid");
    }

    [Fact]
    public async Task Result_of_another_contract_is_rejected()
    {
        // Schema-valid, but the ids belong to someone else: the trust boundary must catch it.
        var (_, run) = await SeedAsync();
        var key = PutExampleResult(run.AnalysisRunId, Guid.CreateVersion7());

        var outcome = await HandleAsync(Completed(run.AnalysisRunId, key));

        Assert.Equal(ResultOutcome.Rejected, outcome);
        Assert.Equal(0, await CountClausesAsync(run.AnalysisRunId));
    }

    [Fact]
    public async Task Result_key_outside_the_job_folder_is_rejected()
    {
        var (contract, run) = await SeedAsync();
        var otherJob = Guid.CreateVersion7();
        var key = PutExampleResult(otherJob, contract.ContractId);

        var outcome = await HandleAsync(Completed(run.AnalysisRunId, key));

        Assert.Equal(ResultOutcome.Rejected, outcome);
    }

    [Fact]
    public async Task Worker_failure_returns_the_contract_to_its_previous_status()
    {
        var (contract, run) = await SeedAsync();

        var outcome = await HandleAsync(new AnalysisResultMessage(AnalysisEventTypes.Failed, run.AnalysisRunId, null, null, "too many pages", "ERR-UPL-004"));

        Assert.Equal(ResultOutcome.Applied, outcome);
        await AssertFailedAsync(run.AnalysisRunId, contract.ContractId, "ERR-UPL-004");
    }

    [Fact]
    public async Task Progress_updates_the_current_step()
    {
        var (_, run) = await SeedAsync();

        await HandleAsync(new AnalysisResultMessage(AnalysisEventTypes.Progress, run.AnalysisRunId, "evaluating", null, null, null));

        await using var scope = factory.CreateSystemScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var saved = await db.AnalysisRuns.SingleAsync(r => r.AnalysisRunId == run.AnalysisRunId, Ct);
        Assert.Equal(AnalysisStep.Evaluating, saved.CurrentStep);
    }

    [Fact]
    public async Task Message_without_job_id_is_poison()
    {
        await Assert.ThrowsAsync<PoisonMessageException>(() =>
            HandleAsync(new AnalysisResultMessage(AnalysisEventTypes.Completed, null, null, null, null, null)));
    }

    [Fact]
    public async Task Rerun_replaces_the_previous_result_of_the_same_version()
    {
        // FR-ANA-015
        var (contract, firstRun) = await SeedAsync();
        await HandleAsync(Completed(firstRun.AnalysisRunId, PutExampleResult(firstRun.AnalysisRunId, contract.ContractId)));

        Guid secondRunId;
        await using (var scope = factory.CreateSystemScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var tracked = await db.Contracts.Include(c => c.Versions).ThenInclude(v => v.Context).SingleAsync(c => c.ContractId == contract.ContractId, Ct);
            var versionId = tracked.LatestVersion.ContractVersionId;
            tracked.MarkAnalysisStarted(versionId, DateTimeOffset.UtcNow);
            var secondRun = AnalysisRun.Start(contract.ContractId, versionId, DateTimeOffset.UtcNow);
            db.AnalysisRuns.Add(secondRun);
            await db.SaveChangesAsync(Ct);
            secondRunId = secondRun.AnalysisRunId;
        }

        await HandleAsync(Completed(secondRunId, PutExampleResult(secondRunId, contract.ContractId)));

        await using var check = factory.CreateSystemScope();
        var checkDb = check.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.False(await checkDb.AnalysisRuns.AnyAsync(r => r.AnalysisRunId == firstRun.AnalysisRunId, Ct));
        Assert.Equal(8, await CountClausesAsync(secondRunId));
    }

    [Fact]
    public async Task Sweeper_fails_jobs_silent_for_more_than_20_minutes()
    {
        var clock = new FakeTimeProvider(DateTimeOffset.UtcNow);
        var (contract, run) = await SeedAsync(clock.GetUtcNow());
        clock.Advance(TimeSpan.FromMinutes(1));
        await ReportProgressAsync(run.AnalysisRunId, clock.GetUtcNow());
        clock.Advance(TimeSpan.FromMinutes(21));

        Assert.True(await SweepAsync(clock) >= 1);

        await AssertFailedAsync(run.AnalysisRunId, contract.ContractId, "20 minutes");
    }

    [Fact]
    public async Task Sweeper_fails_jobs_no_worker_picked_up_within_10_minutes()
    {
        var clock = new FakeTimeProvider(DateTimeOffset.UtcNow);
        var (contract, run) = await SeedAsync(clock.GetUtcNow());
        clock.Advance(TimeSpan.FromMinutes(11));

        Assert.True(await SweepAsync(clock) >= 1);

        await AssertFailedAsync(run.AnalysisRunId, contract.ContractId, "did not pick up the job within 10 minutes");
    }

    [Fact]
    public async Task Sweeper_leaves_picked_up_jobs_running_until_20_minutes_of_silence()
    {
        var clock = new FakeTimeProvider(DateTimeOffset.UtcNow);
        var (_, run) = await SeedAsync(clock.GetUtcNow());
        clock.Advance(TimeSpan.FromMinutes(1));
        await ReportProgressAsync(run.AnalysisRunId, clock.GetUtcNow());
        clock.Advance(TimeSpan.FromMinutes(11));

        await SweepAsync(clock);

        await using var scope = factory.CreateSystemScope();
        var stored = await scope.ServiceProvider.GetRequiredService<AppDbContext>().AnalysisRuns.SingleAsync(r => r.AnalysisRunId == run.AnalysisRunId, Ct);
        Assert.Equal(RunStatus.Running, stored.RunStatus);
    }

    private async Task<int> SweepAsync(TimeProvider clock)
    {
        await using var scope = factory.CreateSystemScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await new StaleRunSweeper(db, clock, NullLogger<StaleRunSweeper>.Instance).SweepAsync(Ct);
    }

    private async Task ReportProgressAsync(Guid runId, DateTimeOffset now)
    {
        await using var scope = factory.CreateSystemScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var run = await db.AnalysisRuns.SingleAsync(r => r.AnalysisRunId == runId, Ct);
        run.ReportProgress(AnalysisStep.SplittingClauses, now);
        await db.SaveChangesAsync(Ct);
    }

    private async Task<(Contract Contract, AnalysisRun Run)> SeedAsync(DateTimeOffset? now = null)
    {
        await using var scope = factory.CreateSystemScope();
        return await Seed.RunningJobAsync(scope.ServiceProvider.GetRequiredService<AppDbContext>(), now ?? DateTimeOffset.UtcNow);
    }

    private string PutExampleResult(Guid jobId, Guid contractId)
    {
        var key = $"results/{jobId}/extraction.json";
        factory.Storage.PutText(key, Seed.ExampleExtractionJson(jobId, contractId));
        return key;
    }

    private static AnalysisResultMessage Completed(Guid jobId, string key) =>
        new(AnalysisEventTypes.Completed, jobId, null, key, null, null);

    private async Task<ResultOutcome> HandleAsync(AnalysisResultMessage message)
    {
        // Same as the SQS consumer: one DI scope per message.
        await using var scope = factory.CreateSystemScope();
        return await scope.ServiceProvider.GetRequiredService<AnalysisResultHandler>().HandleAsync(message, Ct);
    }

    private async Task<int> CountClausesAsync(Guid runId)
    {
        await using var scope = factory.CreateSystemScope();
        return await scope.ServiceProvider.GetRequiredService<AppDbContext>().Clauses.CountAsync(c => c.AnalysisRunId == runId, Ct);
    }

    private async Task AssertFailedAsync(Guid runId, Guid contractId, string reasonFragment)
    {
        await using var scope = factory.CreateSystemScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var run = await db.AnalysisRuns.SingleAsync(r => r.AnalysisRunId == runId, Ct);
        Assert.Equal(RunStatus.Failed, run.RunStatus);
        Assert.Contains(reasonFragment, run.FailureReason, StringComparison.Ordinal);
        Assert.Equal(0, await db.Clauses.CountAsync(c => c.AnalysisRunId == runId, Ct));
        var contract = await db.Contracts.SingleAsync(c => c.ContractId == contractId, Ct);
        Assert.Equal(ApprovalStatus.New, contract.ApprovalStatus);
    }
}
