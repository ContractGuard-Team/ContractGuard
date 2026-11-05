using ContractGuard.Application.Abstractions;
using ContractGuard.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;

namespace ContractGuard.IntegrationTests;

// One real PostgreSQL (same pgvector image as compose.yaml) for the whole test run, with all
// migrations applied, and the API hosted in memory on top of it. S3 is replaced by an in-memory
// store; SQS is not needed because the background consumer is switched off.
public sealed class ApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("pgvector/pgvector:0.8.7-pg18").Build();

    public InMemoryObjectStorage Storage { get; } = new();

    public string ConnectionString => _postgres.GetConnectionString();

    public async ValueTask InitializeAsync()
    {
        await _postgres.StartAsync();

        // Apply migrations the same way `dotnet ef database update` does.
        await using var scope = Services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.MigrateAsync();
    }

    public new async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();
        await _postgres.DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("Database:ConnectionString", ConnectionString);
        builder.UseSetting("Storage:BucketName", "test-bucket");
        builder.UseSetting("Aws:ServiceUrl", "http://localhost:4566");
        builder.UseSetting("BackgroundJobs:Enabled", "false");

        builder.ConfigureTestServices(services =>
        {
            services.AddSingleton<IObjectStorage>(Storage);
        });
    }
}
