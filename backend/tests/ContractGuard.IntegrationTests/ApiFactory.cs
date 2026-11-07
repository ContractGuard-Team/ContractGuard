using ContractGuard.Application.Abstractions;
using ContractGuard.Application.Analysis;
using ContractGuard.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;

namespace ContractGuard.IntegrationTests;

// One real PostgreSQL (same pgvector image as compose.yaml) for the whole test run, with all
// migrations applied, and the API hosted in memory on top of it. S3, SQS and SMTP are replaced by
// in-memory stand-ins; the background consumer is switched off.
public sealed class ApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("pgvector/pgvector:0.8.7-pg18").Build();

    public InMemoryObjectStorage Storage { get; } = new();
    public RecordingJobQueue Queue { get; } = new();
    public RecordingEmailSender Emails { get; } = new();

    public string ConnectionString => _postgres.GetConnectionString();

    // A scope like the ones of the background jobs, which see every workspace.
    public AsyncServiceScope CreateSystemScope()
    {
        var scope = Services.CreateAsyncScope();
        scope.ServiceProvider.GetRequiredService<CurrentWorkspace>().RunAsSystem();
        return scope;
    }

    public ApiClient CreateApiClient() => new(CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true }));

    // A client of a second API on the same database and stand-ins, with one setting changed.
    public ApiClient CreateApiClientWithSetting(string key, string value) =>
        new(WithWebHostBuilder(builder => builder.UseSetting(key, value))
            .CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true }));

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
        builder.UseSetting("Email:SmtpHost", "localhost");
        builder.UseSetting("Email:FromAddress", "no-reply@example.com");
        builder.UseSetting("Email:FrontendBaseUrl", "http://localhost:5173");

        builder.ConfigureTestServices(services =>
        {
            services.AddSingleton<IObjectStorage>(Storage);
            services.AddSingleton<IAnalysisJobQueue>(Queue);
            services.AddSingleton<IEmailSender>(Emails);
        });
    }
}
