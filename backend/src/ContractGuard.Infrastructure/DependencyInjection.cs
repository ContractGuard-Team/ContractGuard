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
        services.AddOptions<StorageOptions>()
        .Bind(configuration.GetSection(Storage.StorageOptions.SectionName))
        .ValidateDataAnnotations()
        .ValidateOnStart();

        services.AddOptions<QueueOptions>()
        .Bind(configuration.GetSection(QueueOptions.SectionName))
        .ValidateDataAnnotations()
        .ValidateOnStart();

        services.AddOptions<DatabaseOptions>()
        .Bind(configuration.GetSection(DatabaseOptions.SectionName))
        .ValidateDataAnnotations()
        .ValidateOnStart();

        services.AddDbContext<AppDbContext>((serviceProvider, options) =>
        {
            var database = serviceProvider.GetRequiredService<IOptions<DatabaseOptions>>().Value;
            options.UseNpgsql(database.ConnectionString)
            .UseSnakeCaseNamingConvention();
        });

        services.AddHealthChecks()
            .AddDbContextCheck<AppDbContext>("postgres", tags: ["ready"]);

        return services;
    }
}