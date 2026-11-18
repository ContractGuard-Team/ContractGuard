namespace ContractGuard.Application.Chat;

// AI-HOOKUP (F-CHAT-001): the one place the chatbot model plugs in. SD-14 option A implements it in C#
// (retrieve clauses of AnalysisRunId from pgvector, call the AI team's self-hosted model endpoint with their
// prompts/, DEC-GEN-029); option B
// implements it as a client of a Python chat service. Either way the backend keeps: auth, history,
// streaming to the browser, and dropping citations of clauses that are not in the run.
public interface IChatResponder
{
    // False while no model is connected: the API answers ERR-CHAT-001 and the UI says so.
    bool IsAvailable { get; }

    // Streams the answer: text pieces in order, plus citations whenever known.
    // Must only read data of request.AnalysisRunId (BR-CHAT-001, FR-CHAT-006).
    IAsyncEnumerable<ChatChunk> AnswerAsync(ChatRequest request, CancellationToken cancellationToken);
}
