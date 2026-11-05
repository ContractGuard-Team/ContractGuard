using System.Collections.Concurrent;
using Amazon.SQS;

namespace ContractGuard.Infrastructure.Messaging;

// SQS APIs take a queue URL, configuration holds the queue name. The URL is looked up once
// per name and cached for the lifetime of the app (singleton).
internal sealed class QueueUrlProvider(IAmazonSQS sqs)
{
    private readonly ConcurrentDictionary<string, Task<string>> _urls = new();

    public Task<string> GetAsync(string queueName, CancellationToken cancellationToken) =>
        _urls.GetOrAdd(queueName, name => LookupAsync(name, cancellationToken));

    private async Task<string> LookupAsync(string queueName, CancellationToken cancellationToken)
    {
        try
        {
            var response = await sqs.GetQueueUrlAsync(queueName, cancellationToken);
            return response.QueueUrl;
        }
        catch
        {
            // Do not cache a failure (e.g. LocalStack not started yet): the next call retries.
            _urls.TryRemove(queueName, out _);
            throw;
        }
    }
}
