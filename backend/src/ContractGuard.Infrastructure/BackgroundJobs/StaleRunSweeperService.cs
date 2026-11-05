using ContractGuard.Application.Analysis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ContractGuard.Infrastructure.BackgroundJobs;

// Runs StaleRunSweeper once a minute.
internal sealed partial class StaleRunSweeperService(
    IServiceScopeFactory scopeFactory,
    IOptions<BackgroundJobsOptions> jobs,
    ILogger<StaleRunSweeperService> logger) : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!jobs.Value.Enabled)
        {
            return;
        }

        using var timer = new PeriodicTimer(Interval);
        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                try
                {
                    await using var scope = scopeFactory.CreateAsyncScope();
                    await scope.ServiceProvider.GetRequiredService<StaleRunSweeper>().SweepAsync(stoppingToken);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    LogSweepFailed(ex);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Normal shutdown.
        }
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "Stale run sweep failed")]
    private partial void LogSweepFailed(Exception ex);
}
