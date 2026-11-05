using ContractGuard.Application.Abstractions;
using ContractGuard.Application.Analysis;
using ContractGuard.Application.Analysis.Extraction;
using ContractGuard.Infrastructure.Aws;
using ContractGuard.Infrastructure.BackgroundJobs;
using ContractGuard.Infrastructure.JsonSchemas;
using ContractGuard.Infrastructure.Messaging;
using ContractGuard.Infrastructure.Persistence;
using ContractGuard.Infrastructure.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace ContractGuard.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddSingleton(TimeProvider.System);

        AddOptionsSection<StorageOptions>(services, configuration, StorageOptions.SectionName);
        AddOptionsSection<QueueOptions>(services, configuration, QueueOptions.SectionName);
        AddOptionsSection<DatabaseOptions>(services, configuration, DatabaseOptions.SectionName);
        AddOptionsSection<AwsOptions>(services, configuration, AwsOptions.SectionName);
        AddOptionsSection<BackgroundJobsOptions>(services, configuration, BackgroundJobsOptions.SectionName);

        AddPersistence(services);
        AddAws(services);
        AddAnalysis(services);

        return services;
    }

    private static void AddOptionsSection<TOptions>(IServiceCollection services, IConfiguration configuration, string sectionName)
        where TOptions : class
    {
        services.AddOptions<TOptions>()
            .Bind(configuration.GetSection(sectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();
    }

    private static void AddPersistence(IServiceCollection services)
    {
        services.AddDbContext<AppDbContext>((serviceProvider, options) =>
        {
            var database = serviceProvider.GetRequiredService<IOptions<DatabaseOptions>>().Value;
            options.UseNpgsql(database.ConnectionString)
                .UseSnakeCaseNamingConvention();
        });

        // Same scoped instance whether asked for as AppDbContext or as the Application port.
        services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<AppDbContext>());

        services.AddHealthChecks()
            .AddDbContextCheck<AppDbContext>("postgres", tags: ["ready"]);
    }

    private static void AddAws(IServiceCollection services)
    {
        // AWS SDK clients are thread-safe and hold an HTTP connection pool: one instance per app.
        services.AddSingleton(sp =>
        {
            var aws = sp.GetRequiredService<IOptions<AwsOptions>>().Value;
            return AwsClientFactory.CreateS3(aws, aws.ServiceUrl);
        });
        services.AddSingleton(sp => AwsClientFactory.CreateSqs(sp.GetRequiredService<IOptions<AwsOptions>>().Value));
        services.AddSingleton<QueueUrlProvider>();
        services.AddSingleton<IAnalysisJobQueue, SqsAnalysisJobQueue>();
        services.AddSingleton<IObjectStorage, S3ObjectStorage>();

        services.AddHealthChecks()
            .AddCheck<S3BucketHealthCheck>("s3", tags: ["ready"])
            .AddCheck<SqsQueuesHealthCheck>("sqs", tags: ["ready"]);
    }

    private static void AddAnalysis(IServiceCollection services)
    {
        services.AddSingleton<IExtractionReader, ExtractionReader>();
        services.AddHostedService<AnalysisResultsConsumer>();
        services.AddHostedService<StaleRunSweeperService>();
    }
}
