using System.Runtime.CompilerServices;
using System.Text;
using ContractGuard.Application.Abstractions;
using ContractGuard.Domain.Analysis;
using ContractGuard.Domain.Chat;
using ContractGuard.Domain.Common;
using ContractGuard.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ContractGuard.Application.Chat;

// F-CHAT-001, F-CHAT-003: asks the model about one contract version and keeps the shared conversation.
// The question is stored before the model is called, the answer only once it is complete, so a broken
// stream leaves the question visible without a half answer.
public sealed partial class ChatService(
    IApplicationDbContext db,
    ICurrentWorkspace workspace,
    IChatResponder responder,
    TimeProvider clock,
    ILogger<ChatService> logger)
{
    private const int HistoryTurns = 10;
    private const int ThreadLimit = 200;

    public async Task<ChatThread> GetThreadAsync(Guid contractId, CancellationToken cancellationToken)
    {
        if (!await db.Contracts.AnyAsync(c => c.ContractId == contractId, cancellationToken))
        {
            throw new DomainException(ErrorCodes.NotFound, "Contract not found.");
        }

        // shortcut: newest 200 messages only, page backwards if conversations ever get that long.
        var messages = await db.ChatMessages
            .Where(m => m.ContractId == contractId)
            .OrderByDescending(m => m.CreatedAt)
            .Take(ThreadLimit)
            .Join(db.ContractVersions, m => m.ContractVersionId, v => v.ContractVersionId, (m, v) => new ChatMessageItem(
                m.ChatMessageId, m.Role, m.AuthorName, m.Content, m.Citations, v.VersionNumber, m.CreatedAt))
            .ToListAsync(cancellationToken);
        messages.Reverse();
        return new ChatThread(responder.IsAvailable, messages);
    }

    // Everything that can be refused is checked before the first event, so it is still a normal HTTP error.
    public async Task<PreparedQuestion> PrepareAsync(Guid contractId, Guid? contractVersionId, string question, string authorName, UiLanguage language, CancellationToken cancellationToken)
    {
        question = question.Trim();
        if (question.Length is 0 or > ChatMessage.QuestionMaxLength)
        {
            throw new DomainException(ErrorCodes.Validation, $"A question has 1 to {ChatMessage.QuestionMaxLength} characters.");
        }

        var contract = await db.Contracts
            .Include(c => c.Versions)
            .SingleOrDefaultAsync(c => c.ContractId == contractId, cancellationToken)
            ?? throw new DomainException(ErrorCodes.NotFound, "Contract not found.");
        var version = contractVersionId is { } id ? contract.GetVersion(id) : contract.LatestVersion;

        var runId = await db.AnalysisRuns
            .Where(r => r.ContractVersionId == version.ContractVersionId && r.RunStatus == RunStatus.Completed)
            .OrderByDescending(r => r.StartedAt)
            .Select(r => (Guid?)r.AnalysisRunId)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new DomainException(ErrorCodes.ChatFailed, "This version has no analysis result to ask about.");

        if (!responder.IsAvailable)
        {
            throw new DomainException(ErrorCodes.ChatFailed, "The assistant is not connected.");
        }

        var history = await db.ChatMessages
            .Where(m => m.ContractVersionId == version.ContractVersionId)
            .OrderByDescending(m => m.CreatedAt)
            .Take(HistoryTurns)
            .Select(m => new ChatTurn(m.Role, m.Content))
            .ToListAsync(cancellationToken);
        history.Reverse();

        var message = ChatMessage.Question(contract.ContractId, version.ContractVersionId, workspace.UserId, authorName, question, clock.GetUtcNow());
        db.ChatMessages.Add(message);
        await db.SaveChangesAsync(cancellationToken);

        return new PreparedQuestion(message, new ChatRequest(contract.ContractId, version.ContractVersionId, runId, question, history, language));
    }

    public async IAsyncEnumerable<ChatEvent> StreamAnswerAsync(PreparedQuestion prepared, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        yield return new ChatEvent("question", MessageId: prepared.Question.ChatMessageId);

        // Trust boundary, like the extraction result: a citation must name a clause of this run.
        var clauseKeys = (await db.Clauses
            .Where(c => c.AnalysisRunId == prepared.Request.AnalysisRunId)
            .Select(c => c.ClauseKey)
            .ToListAsync(cancellationToken)).ToHashSet(StringComparer.Ordinal);

        var text = new StringBuilder();
        var citations = new List<ChatCitation>();
        var failed = false;
        var chunks = responder.AnswerAsync(prepared.Request, cancellationToken).GetAsyncEnumerator(cancellationToken);
        try
        {
            while (true)
            {
                try
                {
                    if (!await chunks.MoveNextAsync())
                    {
                        break;
                    }
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    LogAnswerFailed(ex, prepared.Request.ContractId);
                    failed = true;
                    break;
                }

                var chunk = chunks.Current;
                if (chunk.Text is { Length: > 0 } piece)
                {
                    text.Append(piece);
                    yield return new ChatEvent("token", Text: piece);
                }

                if (chunk.Citation is { } citation && clauseKeys.Contains(citation.ClauseKey) && citations.All(c => c.ClauseKey != citation.ClauseKey))
                {
                    citations.Add(citation);
                    yield return new ChatEvent("citation", Citation: citation);
                }
            }
        }
        finally
        {
            await chunks.DisposeAsync();
        }

        if (failed || text.Length == 0)
        {
            yield return new ChatEvent("error", Code: ErrorCodes.ChatFailed);
            yield break;
        }

        var answer = ChatMessage.Answer(prepared.Request.ContractId, prepared.Request.ContractVersionId, text.ToString(), citations, clock.GetUtcNow());
        db.ChatMessages.Add(answer);
        await db.SaveChangesAsync(CancellationToken.None);
        yield return new ChatEvent("done", MessageId: answer.ChatMessageId);
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "Chat answer for contract {ContractId} failed")]
    private partial void LogAnswerFailed(Exception ex, Guid contractId);
}
