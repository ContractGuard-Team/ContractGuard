using System.Text.Json;
using Amazon.SQS;
using Amazon.SQS.Model;
using ContractGuard.Application.Analysis;
using ContractGuard.Infrastructure.BackgroundJobs;
using ContractGuard.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ContractGuard.Infrastructure.Messaging;

// Long-running loop that reads analysis-results and hands each message to AnalysisResultHandler.
//
// Delivery rules (SD-13):
// - A message is deleted only after it was handled (or deliberately ignored/rejected).
// - On an unexpected error the message is NOT deleted. It becomes visible again after the queue's
//   visibility timeout and is retried; after 3 receives SQS moves it to analysis-results-dlq.
// - A message that cannot be understood at all (poison) is also left alone, so it ends in the DLQ.
internal sealed partial class AnalysisResultsConsumer(
    IAmazonSQS sqs,
    QueueUrlProvider queueUrls,
    IOptions<QueueOptions> queues,
    IOptions<BackgroundJobsOptions> jobs,
    IServiceScopeFactory scopeFactory,
    ILogger<AnalysisResultsConsumer> logger) : BackgroundService
{
    private static readonly TimeSpan ErrorBackoff = TimeSpan.FromSeconds(5);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!jobs.Value.Enabled)
        {
            return;
        }

        LogStarted(queues.Value.AnalysisResults);
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var queueUrl = await queueUrls.GetAsync(queues.Value.AnalysisResults, stoppingToken);

                // Long polling: SQS holds the request up to 20 s until a message arrives.
                // Far fewer empty responses (and requests) than polling every second.
                var response = await sqs.ReceiveMessageAsync(
                    new ReceiveMessageRequest
                    {
                        QueueUrl = queueUrl,
                        MaxNumberOfMessages = 10,
                        WaitTimeSeconds = 20,
                        MessageSystemAttributeNames = ["ApproximateReceiveCount"]
                    },
                    stoppingToken);

                // AWS SDK v4 returns null instead of an empty list when there is no message.
                foreach (var message in response.Messages ?? [])
                {
                    await ProcessAsync(queueUrl, message, stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                // SQS unreachable (e.g. LocalStack restarting): wait a little instead of spinning.
                LogReceiveFailed(ex);
                try
                {
                    await Task.Delay(ErrorBackoff, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }
    }

    private async Task ProcessAsync(string queueUrl, Message message, CancellationToken stoppingToken)
    {
        var receiveCount = message.Attributes?.GetValueOrDefault("ApproximateReceiveCount") ?? "?";
        LogReceived(message.MessageId, receiveCount, message.Body.Length > MaxLoggedBody ? message.Body[..MaxLoggedBody] + "…" : message.Body);
        try
        {
            // Tolerate a UTF-8 byte order mark: some tools (Windows PowerShell 5.1) add one.
            var body = JsonSerializer.Deserialize<AnalysisResultMessage>(message.Body.TrimStart('\uFEFF'), MessageJson.Options)
                ?? throw new PoisonMessageException("Empty message body.");

            // One DI scope per message: a fresh DbContext, like one HTTP request.
            await using var scope = scopeFactory.CreateAsyncScope();
            scope.ServiceProvider.GetRequiredService<CurrentWorkspace>().RunAsSystem();
            var handler = scope.ServiceProvider.GetRequiredService<AnalysisResultHandler>();
            var outcome = await handler.HandleAsync(body, stoppingToken);

            await sqs.DeleteMessageAsync(queueUrl, message.ReceiptHandle, stoppingToken);
            LogHandled(message.MessageId, outcome);
        }
        catch (Exception ex) when (ex is JsonException or PoisonMessageException)
        {
            LogPoison(message.MessageId, receiveCount, ex.Message);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Shutting down: the message becomes visible again and another instance takes it.
        }
        catch (Exception ex)
        {
            LogHandlingFailed(ex, message.MessageId, receiveCount);
        }
    }

    // Debug only (enabled in appsettings.Development.json): chat_answered carries text drawn from the contract.
    private const int MaxLoggedBody = 4000;

    [LoggerMessage(Level = LogLevel.Debug, Message = "Received SQS message {MessageId} (receive #{ReceiveCount}): {Body}")]
    private partial void LogReceived(string messageId, string receiveCount, string body);

    [LoggerMessage(Level = LogLevel.Information, Message = "Consuming {QueueName}")]
    private partial void LogStarted(string queueName);

    [LoggerMessage(Level = LogLevel.Information, Message = "SQS message {MessageId} handled: {Outcome}")]
    private partial void LogHandled(string messageId, ResultOutcome outcome);

    [LoggerMessage(Level = LogLevel.Error, Message = "SQS message {MessageId} is malformed (receive #{ReceiveCount}), left for the DLQ: {Reason}")]
    private partial void LogPoison(string messageId, string receiveCount, string reason);

    [LoggerMessage(Level = LogLevel.Error, Message = "SQS message {MessageId} failed (receive #{ReceiveCount}); it will be retried")]
    private partial void LogHandlingFailed(Exception ex, string messageId, string receiveCount);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Receiving from SQS failed; retrying shortly")]
    private partial void LogReceiveFailed(Exception ex);
}
