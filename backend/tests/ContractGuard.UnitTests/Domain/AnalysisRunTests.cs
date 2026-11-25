using ContractGuard.Domain.Analysis;

namespace ContractGuard.UnitTests.Domain;

public sealed class AnalysisRunTests
{
    private static readonly DateTimeOffset Now = TestData.Now;
    private static readonly AnalysisResults NoResults = new([], [], null, [], null);

    [Fact]
    public void Starts_running_at_reading_file_with_correlation_id()
    {
        var run = AnalysisRun.Start(Guid.CreateVersion7(), Guid.CreateVersion7(), Now);

        Assert.Equal(RunStatus.Running, run.RunStatus);
        Assert.Equal(AnalysisStep.ReadingFile, run.CurrentStep);
        Assert.Equal(run.AnalysisRunId, run.CorrelationId);
    }

    [Fact]
    public void Progress_never_goes_back()
    {
        var run = AnalysisRun.Start(Guid.CreateVersion7(), Guid.CreateVersion7(), Now);

        run.ReportProgress(AnalysisStep.Evaluating, Now.AddSeconds(10));
        run.ReportProgress(AnalysisStep.SplittingClauses, Now.AddSeconds(20));

        Assert.Equal(AnalysisStep.Evaluating, run.CurrentStep);
        Assert.Equal(Now.AddSeconds(20), run.LastEventAt);
    }

    [Fact]
    public void Complete_stores_clauses_and_trace()
    {
        var run = AnalysisRun.Start(Guid.CreateVersion7(), Guid.CreateVersion7(), Now);

        var applied = run.Complete(new AnalysisTrace("2.0.0", "model-a", "prompts-1", null), [Clause("cl-1", "C05")], NoResults, Now);

        Assert.True(applied);
        Assert.Equal(RunStatus.Completed, run.RunStatus);
        Assert.Equal(AnalysisStep.Completed, run.CurrentStep);
        Assert.Equal("model-a", run.ModelVersion);
        Assert.Single(run.Clauses);
    }

    [Fact]
    public void Terminal_run_ignores_late_events()
    {
        var run = AnalysisRun.Start(Guid.CreateVersion7(), Guid.CreateVersion7(), Now);
        run.Complete(new AnalysisTrace("2.0.0", null, null, null), [Clause("cl-1", "C01")], NoResults, Now);

        Assert.False(run.ReportProgress(AnalysisStep.Scoring, Now));
        Assert.False(run.Fail("late failure", Now));
        Assert.False(run.Complete(new AnalysisTrace("2.0.0", null, null, null), [], NoResults, Now));
        Assert.Equal(RunStatus.Completed, run.RunStatus);
        Assert.Single(run.Clauses);
    }

    [Fact]
    public void Fail_drops_partial_results()
    {
        var run = AnalysisRun.Start(Guid.CreateVersion7(), Guid.CreateVersion7(), Now);

        run.Fail("worker crashed", Now);

        Assert.Equal(RunStatus.Failed, run.RunStatus);
        Assert.Equal("worker crashed", run.FailureReason);
        Assert.Empty(run.Clauses);
    }

    [Theory]
    [InlineData("C00")]
    [InlineData("C22")]
    [InlineData("c01")]
    [InlineData("X01")]
    public void Unknown_clause_type_is_rejected(string clauseType)
    {
        var run = AnalysisRun.Start(Guid.CreateVersion7(), Guid.CreateVersion7(), Now);

        Assert.Throws<ArgumentException>(() =>
            run.Complete(new AnalysisTrace("2.0.0", null, null, null), [Clause("cl-1", clauseType)], NoResults, Now));
    }

    [Fact]
    public void Cancel_ends_a_running_run_and_later_results_are_ignored()
    {
        var run = AnalysisRun.Start(Guid.CreateVersion7(), Guid.CreateVersion7(), Now);

        Assert.True(run.Cancel(Now.AddSeconds(5)));
        Assert.Equal(RunStatus.Cancelled, run.RunStatus);
        Assert.Equal(Now.AddSeconds(5), run.FinishedAt);

        Assert.False(run.Cancel(Now.AddSeconds(6)));
        Assert.False(run.Complete(new AnalysisTrace("2.0.0", null, null, null), [], NoResults, Now.AddSeconds(7)));
        Assert.False(run.Fail("late", Now.AddSeconds(8)));
        Assert.Equal(RunStatus.Cancelled, run.RunStatus);
    }

    [Fact]
    public void A_finished_run_cannot_be_cancelled()
    {
        var run = AnalysisRun.Start(Guid.CreateVersion7(), Guid.CreateVersion7(), Now);
        run.Complete(new AnalysisTrace("2.0.0", null, null, null), [], NoResults, Now);

        Assert.False(run.Cancel(Now.AddSeconds(1)));
        Assert.Equal(RunStatus.Completed, run.RunStatus);
    }

    private static ClauseData Clause(string key, string type) =>
        new(key, 1, "1", "Heading", "Clause text.", type, new ClauseLocation(1, 1, [new PageRegion(1, 0.1, 0.1, 0.8, 0.1)]));
}
