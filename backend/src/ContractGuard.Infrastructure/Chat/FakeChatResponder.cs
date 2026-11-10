using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using ContractGuard.Application.Abstractions;
using ContractGuard.Application.Chat;
using Microsoft.EntityFrameworkCore;

namespace ContractGuard.Infrastructure.Chat;

// Development and e2e only (Chat:Responder = "fake", refused in Production). Not AI: it picks the clause
// sharing the most words with the question and streams a fixed sentence citing it, so the whole path
// (SSE, history, citations, jump to clause) can be tested before the real model exists.
internal sealed partial class FakeChatResponder(IApplicationDbContext db) : IChatResponder
{
    public bool IsAvailable => true;

    public async IAsyncEnumerable<ChatChunk> AnswerAsync(ChatRequest request, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var clauses = await db.Clauses
            .Where(c => c.AnalysisRunId == request.AnalysisRunId)
            .Select(c => new { c.ClauseKey, c.ClauseNumber, c.Heading, c.ClauseText })
            .ToListAsync(cancellationToken);
        var words = Words(request.Question);
        var best = clauses
            .Select(c => new { Clause = c, Score = Words($"{c.Heading} {c.ClauseText}").Count(words.Contains) })
            .Where(x => x.Score > 0)
            .MaxBy(x => x.Score)?.Clause;

        var answer = best is null
            ? "[Giả lập, chưa có AI] Hợp đồng không đề cập nội dung này."
            : $"[Giả lập, chưa có AI] Nội dung liên quan nằm ở điều {best.ClauseNumber} ({best.Heading}). Đây là câu trả lời mẫu để kiểm thử luồng chat.";
        foreach (var piece in answer.Split(' '))
        {
            await Task.Delay(15, cancellationToken);
            yield return ChatChunk.Of(piece + " ");
        }

        if (best is not null)
        {
            yield return ChatChunk.Cite(best.ClauseKey);
        }
    }

    private static HashSet<string> Words(string text) =>
        WordPattern().Matches(text.ToLowerInvariant()).Select(m => m.Value).Where(w => w.Length > 3).ToHashSet();

    [GeneratedRegex(@"\p{L}+")]
    private static partial Regex WordPattern();
}
