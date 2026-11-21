using System.Collections.Concurrent;

namespace ContractGuard.Application.Chat;

// Hands a worker's answer (read by the analysis-results consumer) to the HTTP request waiting for it.
// shortcut: in memory, so it only works with one API instance; with several, store answers in the database and poll.
public sealed class ChatAnswerWaiter
{
    private readonly ConcurrentDictionary<Guid, TaskCompletionSource<WorkerChatAnswer>> _pending = new();

    public Task<WorkerChatAnswer> Register(Guid jobId) =>
        _pending.GetOrAdd(jobId, _ => new TaskCompletionSource<WorkerChatAnswer>(TaskCreationOptions.RunContinuationsAsynchronously)).Task;

    // False when nobody waits for this id (timed out, or another instance asked).
    public bool TryComplete(Guid jobId, WorkerChatAnswer answer) =>
        _pending.TryRemove(jobId, out var waiting) && waiting.TrySetResult(answer);

    public bool TryFail(Guid jobId, string reason) =>
        _pending.TryRemove(jobId, out var waiting) && waiting.TrySetException(new InvalidOperationException(reason));

    public void Remove(Guid jobId) => _pending.TryRemove(jobId, out _);
}
