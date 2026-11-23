using ContractGuard.Application.Abstractions;
using ContractGuard.Application.Analysis.Extraction;
using ContractGuard.Application.Chat;
using ContractGuard.Domain.Analysis;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ContractGuard.Application.Analysis;

// Applies one analysis-results message to the database. Scoped: one instance per message,
// sharing the scoped DbContext. Kept apart from the SQS loop so it can be unit tested.
public sealed partial class AnalysisResultHandler(
    IApplicationDbContext db,
    IExtractionReader extractionReader,
    AnalysisResultProcessor resultProcessor,
    ChatAnswerWaiter chatAnswers,
    TimeProvider clock,
    ILogger<AnalysisResultHandler> logger)
{
    public async Task<ResultOutcome> HandleAsync(AnalysisResultMessage message, CancellationToken cancellationToken)
    {
        if (message.JobId is not { } jobId || jobId == Guid.Empty)
        {
            throw new PoisonMessageException("Message has no job_id.");
        }

        // Every log line written while handling this message carries job_id (SD-19, FR-ANA-017).
        using var scope = logger.BeginScope(new Dictionary<string, object> { ["job_id"] = jobId });

        switch (message.EventType)
        {
            case AnalysisEventTypes.Pong:
                LogPong(jobId);
                return ResultOutcome.Applied;
            case AnalysisEventTypes.Progress:
                return await HandleProgressAsync(jobId, message.Step, cancellationToken);
            case AnalysisEventTypes.Failed when chatAnswers.TryFail(jobId, message.FailureReason ?? "The worker could not answer."):
                return ResultOutcome.Applied;
            case AnalysisEventTypes.Failed:
                return await HandleFailedAsync(jobId, message, cancellationToken);
            case AnalysisEventTypes.Completed:
                return await HandleCompletedAsync(jobId, message.ResultKey, cancellationToken);
            case AnalysisEventTypes.ChatAnswered:
                return HandleChatAnswered(jobId, message);
            default:
                throw new PoisonMessageException($"Unknown event_type '{message.EventType}'.");
        }
    }

    private async Task<ResultOutcome> HandleProgressAsync(Guid jobId, string? step, CancellationToken cancellationToken)
    {
        if (!TryParseStep(step, out var parsedStep))
        {
            throw new PoisonMessageException($"Unknown progress step '{step}'.");
        }

        var run = await db.AnalysisRuns.SingleOrDefaultAsync(r => r.AnalysisRunId == jobId, cancellationToken);
        if (run is null)
        {
            LogUnknownJob(jobId);
            return ResultOutcome.Ignored;
        }

        if (!run.ReportProgress(parsedStep, clock.GetUtcNow()))
        {
            LogLateMessage(jobId, AnalysisEventTypes.Progress, run.RunStatus);
            return ResultOutcome.Ignored;
        }

        await db.SaveChangesAsync(cancellationToken);
        LogProgress(jobId, parsedStep);
        return ResultOutcome.Applied;
    }

    // Nobody waiting (timed out, or the question came from another API instance): drop the answer.
    private ResultOutcome HandleChatAnswered(Guid jobId, AnalysisResultMessage message)
    {
        var delivered = message.AnswerText is { Length: > 0 and <= MaxChatAnswerLength } text
            ? chatAnswers.TryComplete(jobId, new WorkerChatAnswer(text, message.CitedClauseIds ?? []))
            : chatAnswers.TryFail(jobId, "The worker sent an empty or too long answer.");
        return delivered ? ResultOutcome.Applied : ResultOutcome.Ignored;
    }

    // contracts/queue-messages.v1.schema.json, chat_answered.answer_text.
    private const int MaxChatAnswerLength = 8000;

    private async Task<ResultOutcome> HandleFailedAsync(Guid jobId, AnalysisResultMessage message, CancellationToken cancellationToken)
    {
        var reason = string.IsNullOrWhiteSpace(message.ErrorCode)
            ? message.FailureReason ?? "The worker reported a failure without a reason."
            : $"{message.ErrorCode}: {message.FailureReason}";
        return await FailRunAsync(jobId, reason, cancellationToken)
            ? ResultOutcome.Applied
            : ResultOutcome.Ignored;
    }

    private async Task<ResultOutcome> HandleCompletedAsync(Guid jobId, string? resultKey, CancellationToken cancellationToken)
    {
        var run = await db.AnalysisRuns.SingleOrDefaultAsync(r => r.AnalysisRunId == jobId, cancellationToken);
        if (run is null)
        {
            LogUnknownJob(jobId);
            return ResultOutcome.Ignored;
        }

        if (run.IsTerminal)
        {
            LogLateMessage(jobId, AnalysisEventTypes.Completed, run.RunStatus);
            return ResultOutcome.Ignored;
        }

        // The worker may only point at its own job folder, never at another tenant's files.
        var expectedPrefix = AnalysisStorageKeys.ResultsPrefix(jobId);
        if (string.IsNullOrWhiteSpace(resultKey) || !resultKey.StartsWith(expectedPrefix, StringComparison.Ordinal) || resultKey.Contains("..", StringComparison.Ordinal))
        {
            return await RejectAsync(jobId, [$"result_key must start with '{expectedPrefix}'."], cancellationToken);
        }

        ExtractionReadResult read;
        try
        {
            read = await extractionReader.ReadAsync(resultKey, cancellationToken);
        }
        catch (StoredObjectNotFoundException)
        {
            return await RejectAsync(jobId, [$"Result file '{resultKey}' does not exist."], cancellationToken);
        }

        if (!read.IsValid)
        {
            return await RejectAsync(jobId, read.Errors, cancellationToken);
        }

        var contract = await db.Contracts
            .Include(c => c.Versions)
            .SingleAsync(c => c.ContractId == run.ContractId, cancellationToken);
        var version = contract.GetVersion(run.ContractVersionId);

        var consistencyErrors = ExtractionConsistencyValidator.Validate(read.Document!, jobId, run.ContractId, version.VersionNumber);
        if (consistencyErrors.Count > 0)
        {
            return await RejectAsync(jobId, consistencyErrors, cancellationToken);
        }

        var document = read.Document!;
        foreach (var warning in ExtractionConsistencyValidator.ScoringWarnings(document))
        {
            LogScoringWarning(jobId, warning);
        }

        var scoringProblems = ExtractionConsistencyValidator.ScoringProblems(document);
        if (scoringProblems.Count > 0)
        {
            // docs/04 P7-01: a scoring block the web cannot show must not cost the whole (paid) analysis.
            foreach (var problem in scoringProblems.Take(10))
            {
                LogScoringDropped(jobId, problem);
            }

            document = document with { Scoring = null };
        }

        var outcome = await resultProcessor.ApplyAsync(run, contract, version, document, cancellationToken);
        if (outcome.Errors.Count > 0)
        {
            return await RejectAsync(jobId, outcome.Errors, cancellationToken);
        }

        await db.SaveChangesAsync(cancellationToken);
        LogCompleted(jobId, document.Clauses.Count);
        return ResultOutcome.Applied;
    }

    private async Task<ResultOutcome> RejectAsync(Guid jobId, IReadOnlyList<string> errors, CancellationToken cancellationToken)
    {
        foreach (var error in errors.Take(20))
        {
            LogInvalidResult(jobId, error);
        }

        var reason = "The analysis result was invalid: " + string.Join(" ", errors.Take(3));
        await FailRunAsync(jobId, reason, cancellationToken);
        return ResultOutcome.Rejected;
    }

    // TR-ANA-003 + TR-CTR-003. Returns false when the run is unknown or already finished.
    private async Task<bool> FailRunAsync(Guid jobId, string reason, CancellationToken cancellationToken)
    {
        // Pending changes from a half-applied result are discarded first (FR-ANA-005).
        db.DiscardChanges();

        var run = await db.AnalysisRuns.Include(r => r.Clauses)
            .SingleOrDefaultAsync(r => r.AnalysisRunId == jobId, cancellationToken);
        if (run is null)
        {
            LogUnknownJob(jobId);
            return false;
        }

        var now = clock.GetUtcNow();
        if (!run.Fail(reason, now))
        {
            LogLateMessage(jobId, AnalysisEventTypes.Failed, run.RunStatus);
            return false;
        }

        var contract = await db.Contracts.SingleAsync(c => c.ContractId == run.ContractId, cancellationToken);
        contract.MarkAnalysisFailed(now);
        await db.SaveChangesAsync(cancellationToken);
        LogFailed(jobId, reason);
        return true;
    }

    private static bool TryParseStep(string? step, out AnalysisStep parsed)
    {
        parsed = default;
        return step is "reading_file" or "splitting_clauses" or "evaluating" or "scoring"
            && Enum.TryParse(step.Replace("_", string.Empty, StringComparison.Ordinal), ignoreCase: true, out parsed);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Worker answered ping for job {JobId}")]
    private partial void LogPong(Guid jobId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Job {JobId} is at step {Step}")]
    private partial void LogProgress(Guid jobId, AnalysisStep step);

    [LoggerMessage(Level = LogLevel.Information, Message = "Job {JobId} completed with {ClauseCount} clauses")]
    private partial void LogCompleted(Guid jobId, int clauseCount);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Job {JobId} failed: {Reason}")]
    private partial void LogFailed(Guid jobId, string reason);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Job {JobId} result rejected: {Error}")]
    private partial void LogInvalidResult(Guid jobId, string error);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Job {JobId} scoring dropped, rest of the result kept: {Problem}")]
    private partial void LogScoringDropped(Guid jobId, string problem);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Job {JobId} scoring kept despite: {Warning}")]
    private partial void LogScoringWarning(Guid jobId, string warning);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Message for unknown job {JobId} ignored")]
    private partial void LogUnknownJob(Guid jobId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Late or duplicate {EventType} for job {JobId} ignored (run is {RunStatus})")]
    private partial void LogLateMessage(Guid jobId, string eventType, RunStatus runStatus);
}
