using System.Runtime.CompilerServices;
using ContractGuard.Application.Abstractions;
using ContractGuard.Application.Analysis;
using Microsoft.EntityFrameworkCore;

namespace ContractGuard.Application.Chat;

// AI-HOOKUP (F-CHAT-001, docs/04 C6): the AI worker answers. The question and the run's clauses and findings
// go to S3, a chat_requested message to the chat-requests queue; the answer comes back as chat_answered on
// analysis-results, where AnalysisResultHandler hands it to ChatAnswerWaiter.
public sealed class WorkerChatResponder(
    IApplicationDbContext db,
    IObjectStorage storage,
    IAnalysisJobQueue queue,
    ChatAnswerWaiter answers) : IChatResponder
{
    // The worker may queue behind another question; past this the user gets ERR-CHAT-001.
    private static readonly TimeSpan AnswerTimeout = TimeSpan.FromMinutes(3);

    public bool IsAvailable => true;

    public async IAsyncEnumerable<ChatChunk> AnswerAsync(ChatRequest request, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var jobId = Guid.CreateVersion7();
        var requestKey = AnalysisStorageKeys.RequestKey(jobId);
        await storage.PutJsonAsync(requestKey, await BuildRequestAsync(jobId, request, cancellationToken), cancellationToken);

        // Registered before publishing, so even an instant answer finds its waiter.
        var answerTask = answers.Register(jobId);
        WorkerChatAnswer answer;
        try
        {
            await queue.PublishAsync(new AnalysisRequestMessage(AnalysisEventTypes.ChatRequested, jobId, requestKey), cancellationToken);
            answer = await answerTask.WaitAsync(AnswerTimeout, cancellationToken);
        }
        finally
        {
            answers.Remove(jobId);
        }

        // The worker does not stream; words keep the UI's typing effect.
        foreach (var word in answer.Text.Split(' '))
        {
            yield return ChatChunk.Of(word + " ");
        }

        foreach (var clauseId in answer.CitedClauseIds)
        {
            yield return ChatChunk.Cite(clauseId);
        }
    }

    private async Task<ChatRequestDocument> BuildRequestAsync(Guid jobId, ChatRequest request, CancellationToken cancellationToken)
    {
        var clauses = await db.Clauses
            .Where(c => c.AnalysisRunId == request.AnalysisRunId)
            .OrderBy(c => c.ClauseOrder)
            .Select(c => new { c.ClauseKey, c.ClauseNumber, c.Heading, c.ClauseText, c.Location })
            .ToListAsync(cancellationToken);
        var findings = await db.Findings
            .Where(f => f.AnalysisRunId == request.AnalysisRunId)
            .Select(f => new ChatRequestFinding(f.FindingKey, f.Title, f.Severity, f.ClauseKey, f.Why, f.Impact))
            .ToListAsync(cancellationToken);
        var recommendation = await db.AnalysisRuns
            .Where(r => r.AnalysisRunId == request.AnalysisRunId)
            .Select(r => r.OverallRecommendation)
            .SingleAsync(cancellationToken);

        return new ChatRequestDocument(
            SchemaVersion: "1.0.0",
            jobId,
            AnalysisJobId: request.AnalysisRunId, // the analysis job id is the run id (AnalysisStarter)
            request.ContractId,
            request.ContractVersionId,
            request.Language,
            request.Question,
            [.. request.History.Select(t => new ChatRequestTurn(t.Role, t.Content))],
            [.. clauses.Select(c => new ChatRequestClause(c.ClauseKey, c.ClauseNumber, c.Heading, c.ClauseText, c.Location.PageStart, c.Location.PageEnd))],
            findings,
            recommendation);
    }
}
