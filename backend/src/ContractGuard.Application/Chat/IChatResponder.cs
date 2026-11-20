namespace ContractGuard.Application.Chat;

// AI-HOOKUP (F-CHAT-001): the one place the chatbot plugs in. Since 2026-11-20 the AI worker answers over
// SQS (WorkerChatResponder, docs/04 C6). Whatever answers, the backend keeps: auth, history,
// streaming to the browser, and dropping citations of clauses that are not in the run.
public interface IChatResponder
{
    // False while no model is connected: the API answers ERR-CHAT-001 and the UI says so.
    bool IsAvailable { get; }

    // Streams the answer: text pieces in order, plus citations whenever known.
    // Must only read data of request.AnalysisRunId (BR-CHAT-001, FR-CHAT-006).
    IAsyncEnumerable<ChatChunk> AnswerAsync(ChatRequest request, CancellationToken cancellationToken);
}
