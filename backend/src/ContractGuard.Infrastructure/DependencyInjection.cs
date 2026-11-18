using ContractGuard.Application.Abstractions;
using ContractGuard.Application.Analysis;
using ContractGuard.Application.Analysis.Extraction;
using ContractGuard.Application.Chat;
using ContractGuard.Infrastructure.Aws;
using ContractGuard.Infrastructure.Chat;
using ContractGuard.Infrastructure.BackgroundJobs;
using ContractGuard.Infrastructure.Email;
using ContractGuard.Infrastructure.JsonSchemas;
using ContractGuard.Infrastructure.Messaging;
using ContractGuard.Infrastructure.Persistence;
using ContractGuard.Infrastructure.Reports;
using ContractGuard.Application.Reports;
using ContractGuard.Infrastructure.Storage;
using Amazon.S3;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
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
        AddOptionsSection<EmailOptions>(services, configuration, EmailOptions.SectionName);
        AddOptionsSection<AnalysisOptions>(services, configuration, AnalysisOptions.SectionName);

        AddPersistence(services);
        AddAws(services);
        AddAnalysis(services);
        AddEmail(services, configuration);

        // AI-HOOKUP (F-CHAT-001): the real model replaces UnconfiguredChatResponder here (docs/06).
        // "fake" is for development and e2e only; Program.cs refuses it in Production.
        if (string.Equals(configuration["Chat:Responder"], "fake", StringComparison.OrdinalIgnoreCase))
        {
            services.AddScoped<IChatResponder, FakeChatResponder>();
        }
        else
        {
            services.AddSingleton<IChatResponder, UnconfiguredChatResponder>();
        }

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

    // Mailpit over SMTP locally and in tests; Amazon SES in the Cloud environment and on AWS.
    private static void AddEmail(IServiceCollection services, IConfiguration configuration)
    {
        if (string.Equals(configuration[$"{EmailOptions.SectionName}:Provider"], EmailOptions.SesProvider, StringComparison.Ordinal))
        {
            services.AddSingleton(sp => AwsClientFactory.CreateSes(sp.GetRequiredService<IOptions<AwsOptions>>().Value));
            services.AddSingleton<IEmailSender, SesEmailSender>();
        }
        else
        {
            services.AddSingleton<IEmailSender, SmtpEmailSender>();
        }
    }

    private static void AddPersistence(IServiceCollection services)
    {
        services.AddScoped<CurrentWorkspace>();
        services.AddScoped<ICurrentWorkspace>(sp => sp.GetRequiredService<CurrentWorkspace>());
        services.AddScoped<IUserDirectory, ContractGuard.Infrastructure.Identity.UserDirectory>();

        services.AddDbContext<AppDbContext>((serviceProvider, options) =>
        {
            var database = serviceProvider.GetRequiredService<IOptions<DatabaseOptions>>().Value;
            options.UseNpgsql(database.ConnectionString, npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory", AppDbContext.AppSchema))
                .UseSnakeCaseNamingConvention()

                // ContractContext has no query filter of its own: it is only reached through its
                // ContractVersion, which has one. EF warns about that shape in general.
                .ConfigureWarnings(w => w.Ignore(CoreEventId.PossibleIncorrectRequiredNavigationWithQueryFilterInteractionWarning));
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
        services.AddKeyedSingleton<IAmazonS3>(S3ObjectStorage.BrowserClientKey, (sp, _) =>
        {
            var aws = sp.GetRequiredService<IOptions<AwsOptions>>().Value;
            return AwsClientFactory.CreateS3(aws, aws.PublicServiceUrl ?? aws.ServiceUrl);
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
        services.AddSingleton<IReportRenderer, PdfReportRenderer>();
        services.AddSingleton<IReportRenderer, DocxReportRenderer>();
        services.AddHostedService<AnalysisResultsConsumer>();
        services.AddHostedService<StaleRunSweeperService>();
    }
}
