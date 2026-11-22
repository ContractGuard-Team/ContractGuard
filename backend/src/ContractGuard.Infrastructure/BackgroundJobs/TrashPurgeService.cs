using ContractGuard.Application.Contracts;
using ContractGuard.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ContractGuard.Infrastructure.BackgroundJobs;

// CR-CTR-001: every hour, deletes for good the contracts that have been in the trash longer than
// Contract.TrashRetention. Runs once at start too, so a redeploy never postpones a purge by an hour.
internal sealed partial class TrashPurgeService(
    IServiceScopeFactory scopeFactory,
    IOptions<BackgroundJobsOptions> jobs,
    ILogger<TrashPurgeService> logger) : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromHours(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!jobs.Value.Enabled)
        {
            return;
        }

        using var timer = new PeriodicTimer(Interval);
        try
        {
            do
            {
                try
                {
                    await using var scope = scopeFactory.CreateAsyncScope();
                    scope.ServiceProvider.GetRequiredService<CurrentWorkspace>().RunAsSystem();
                    await scope.ServiceProvider.GetRequiredService<ContractPurger>().PurgeExpiredAsync(stoppingToken);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    LogPurgeFailed(ex);
                }
            }
            while (await timer.WaitForNextTickAsync(stoppingToken));
        }
        catch (OperationCanceledException)
        {
            // Normal shutdown.
        }
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "Trash purge failed")]
    private partial void LogPurgeFailed(Exception ex);
}
