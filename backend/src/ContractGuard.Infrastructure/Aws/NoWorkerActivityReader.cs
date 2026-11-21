using ContractGuard.Application.Analysis;

namespace ContractGuard.Infrastructure.Aws;

// WorkerActivity:TableArn not set (local, tests, e2e): the page shows only the five steps.
internal sealed class NoWorkerActivityReader : IWorkerActivityReader
{
    public Task<WorkerActivity?> ReadAsync(Guid contractId, Guid jobId, CancellationToken cancellationToken) =>
        Task.FromResult<WorkerActivity?>(null);
}
