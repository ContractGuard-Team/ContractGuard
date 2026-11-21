namespace ContractGuard.Application.Analysis;

// docs/04 C8. Best effort: null when it is not configured, when the worker has no row for this job, or on any error,
// so the page falls back to the five steps it shows from the progress messages.
public interface IWorkerActivityReader
{
    Task<WorkerActivity?> ReadAsync(Guid contractId, Guid jobId, CancellationToken cancellationToken);
}
