using Amazon.SQS;
using ContractGuard.Infrastructure.Messaging;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace ContractGuard.Infrastructure.Aws;

// Readiness: both queues exist and SQS answers. Queue URLs are cached, so the check
// must still make a real call (GetQueueAttributes), otherwise it stays green when SQS is down.
internal sealed class SqsQueuesHealthCheck(IAmazonSQS sqs, QueueUrlProvider queueUrls, IOptions<QueueOptions> queues) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            foreach (var queueName in new[] { queues.Value.AnalysisRequests, queues.Value.AnalysisResults })
            {
                var url = await queueUrls.GetAsync(queueName, cancellationToken);
                await sqs.GetQueueAttributesAsync(url, ["QueueArn"], cancellationToken);
            }

            return HealthCheckResult.Healthy();
        }
        catch (AmazonSQSException ex)
        {
            return HealthCheckResult.Unhealthy($"Queue lookup failed: {ex.ErrorCode}");
        }
    }
}
