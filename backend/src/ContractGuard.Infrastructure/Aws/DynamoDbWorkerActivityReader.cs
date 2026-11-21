using System.Globalization;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using ContractGuard.Application.Analysis;
using Microsoft.Extensions.Logging;

namespace ContractGuard.Infrastructure.Aws;

// docs/04 C8: reads the AI worker's own job table (contract-risk-jobs, in the AI team's account) across accounts.
// The worker keeps one row per contract, keyed "contract:<contract_id>", for the job in active_job_id. The table's
// resource policy only lets this API read the attributes below, never the row's output, which comes from the contract.
internal sealed partial class DynamoDbWorkerActivityReader(IAmazonDynamoDB dynamo, string tableArn, ILogger<DynamoDbWorkerActivityReader> logger)
    : IWorkerActivityReader
{
    // The page polls every 3 seconds: a slow read is skipped rather than holding the request up.
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(2);

    public async Task<WorkerActivity?> ReadAsync(Guid contractId, Guid jobId, CancellationToken cancellationToken)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(Timeout);
        try
        {
            var response = await dynamo.GetItemAsync(new GetItemRequest
            {
                TableName = tableArn,
                Key = new Dictionary<string, AttributeValue> { ["job_id"] = new() { S = $"contract:{contractId}" } },
                ProjectionExpression = "active_job_id, stage, progress"
            }, timeout.Token);
            return Map(response.Item, jobId);
        }
        catch (Exception exception) when (!cancellationToken.IsCancellationRequested)
        {
            // Best effort (IWorkerActivityReader): the page keeps its five steps.
            LogReadFailed(exception.GetType().Name);
            return null;
        }
    }

    // The row describes this job only while active_job_id is its id; a later job of the same contract replaces it.
    internal static WorkerActivity? Map(Dictionary<string, AttributeValue>? item, Guid jobId)
    {
        if (item is null
            || !item.TryGetValue("active_job_id", out var active) || !Guid.TryParse(active.S, out var activeJob) || activeJob != jobId
            || !item.TryGetValue("stage", out var stage) || string.IsNullOrWhiteSpace(stage.S)
            || !item.TryGetValue("progress", out var progress)
            || !double.TryParse(progress.N, NumberStyles.Float, CultureInfo.InvariantCulture, out var percent))
        {
            return null;
        }

        return new WorkerActivity((int)Math.Clamp(Math.Round(percent), 0, 100), stage.S);
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Could not read the worker's progress from DynamoDB: {ErrorType}")]
    private partial void LogReadFailed(string errorType);
}
