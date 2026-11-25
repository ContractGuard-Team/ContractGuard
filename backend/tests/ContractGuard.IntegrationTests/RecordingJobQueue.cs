using System.Collections.Concurrent;
using ContractGuard.Application.Analysis;

namespace ContractGuard.IntegrationTests;

// Stand-in for SQS: remembers what was published. Can pretend the queue is down.
public sealed class RecordingJobQueue : IAnalysisJobQueue
{
    private readonly ConcurrentDictionary<Guid, AnalysisRequestMessage> _published = new();

    public AnalysisRequestMessage? Find(Guid jobId) => _published.GetValueOrDefault(jobId);

    public IReadOnlyList<AnalysisRequestMessage> Published => [.. _published.Values];

    // cancel_requested reuses the job id of its analysis_requested, so it is kept apart instead of replacing it.
    private readonly ConcurrentQueue<Guid> _cancelled = new();

    public IReadOnlyList<Guid> Cancelled => [.. _cancelled];

    // Only for QueueFailureTests. Test classes of one collection never run at the same time.
    public bool FailEverything { get; set; }

    public Task PublishAsync(AnalysisRequestMessage message, CancellationToken cancellationToken)
    {
        if (FailEverything)
        {
            throw new InvalidOperationException("Queue is down.");
        }

        if (message.EventType == AnalysisEventTypes.CancelRequested)
        {
            _cancelled.Enqueue(message.JobId);
        }
        else
        {
            _published[message.JobId] = message;
        }

        return Task.CompletedTask;
    }
}
