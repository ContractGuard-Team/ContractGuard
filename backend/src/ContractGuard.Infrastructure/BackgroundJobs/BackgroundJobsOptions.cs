namespace ContractGuard.Infrastructure.BackgroundJobs;

public sealed class BackgroundJobsOptions
{
    public const string SectionName = "BackgroundJobs";

    // Turns off the SQS results consumer and the stale-run sweeper, e.g. in integration tests
    // that have no SQS, or when a second API instance should not consume.
    public bool Enabled { get; init; } = true;
}
