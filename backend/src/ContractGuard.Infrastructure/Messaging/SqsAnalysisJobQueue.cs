using System.Text.Json;
using Amazon.SQS;
using Amazon.SQS.Model;
using ContractGuard.Application.Analysis;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ContractGuard.Infrastructure.Messaging;

// Adapter: implements the Application port IAnalysisJobQueue with Amazon SQS.
internal sealed partial class SqsAnalysisJobQueue(
    IAmazonSQS sqs,
    QueueUrlProvider queueUrls,
    IOptions<QueueOptions> queues,
    ILogger<SqsAnalysisJobQueue> logger) : IAnalysisJobQueue
{
    public async Task PublishAsync(AnalysisRequestMessage message, CancellationToken cancellationToken)
    {
        var queueUrl = await queueUrls.GetAsync(queues.Value.AnalysisRequests, cancellationToken);
        var body = JsonSerializer.Serialize(
            new
            {
                message_version = AnalysisRequestMessage.MessageVersion,
                event_type = message.EventType,
                job_id = message.JobId,
                request_key = message.RequestKey
            },
            MessageJson.Options);

        var response = await sqs.SendMessageAsync(
            new SendMessageRequest
            {
                QueueUrl = queueUrl,
                MessageBody = body,

                // Attributes let consumers filter or log without parsing the body.
                MessageAttributes = new Dictionary<string, MessageAttributeValue>
                {
                    ["job_id"] = new() { DataType = "String", StringValue = message.JobId.ToString() },
                    ["event_type"] = new() { DataType = "String", StringValue = message.EventType }
                }
            },
            cancellationToken);

        LogPublished(message.EventType, message.JobId, response.MessageId);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Published {EventType} for job {JobId} as SQS message {MessageId}")]
    private partial void LogPublished(string eventType, Guid jobId, string messageId);
}
