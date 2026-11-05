namespace ContractGuard.Application.Analysis;

// The message itself cannot be understood (no job_id, unknown event type). The consumer leaves it
// on the queue so SQS moves it to the DLQ after maxReceiveCount (3) attempts, for a human to inspect.
public sealed class PoisonMessageException(string message) : Exception(message);
