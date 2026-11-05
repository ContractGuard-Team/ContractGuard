using System.Net;
using ContractGuard.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace ContractGuard.IntegrationTests;

[Collection(ApiCollection.Name)]
public sealed class DatabaseTests(ApiFactory factory)
{
    [Fact]
    public async Task Liveness_answers_without_dependencies()
    {
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/health/live", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Migrations_create_every_table_and_the_vector_extension()
    {
        await using var connection = new NpgsqlConnection(factory.ConnectionString);
        await connection.OpenAsync(TestContext.Current.CancellationToken);

        var tables = new List<string>();
        await using (var command = new NpgsqlCommand(
            "SELECT table_name FROM information_schema.tables WHERE table_schema = 'public' ORDER BY table_name", connection))
        await using (var reader = await command.ExecuteReaderAsync(TestContext.Current.CancellationToken))
        {
            while (await reader.ReadAsync(TestContext.Current.CancellationToken))
            {
                tables.Add(reader.GetString(0));
            }
        }

        Assert.Superset(
            new HashSet<string> { "organization", "user_account", "membership", "contract", "contract_version", "contract_context", "analysis_run", "clause" },
            tables.ToHashSet());

        await using var extension = new NpgsqlCommand("SELECT count(*) FROM pg_extension WHERE extname = 'vector'", connection);
        Assert.Equal(1L, await extension.ExecuteScalarAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Model_has_no_pending_changes()
    {
        // Fails when an entity or configuration changed without a new migration.
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        Assert.False(db.Database.HasPendingModelChanges());
    }
}
