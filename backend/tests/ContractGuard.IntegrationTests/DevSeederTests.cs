using ContractGuard.Api.Endpoints;
using ContractGuard.Domain.Analysis;
using ContractGuard.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace ContractGuard.IntegrationTests;

// The development seed produces the same data as a real worker result, and running it twice adds nothing.
[Collection(ApiCollection.Name)]
public sealed class DevSeederTests(ApiFactory factory)
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Seed_creates_demo_accounts_organization_and_analyzed_contracts_once()
    {
        await DevSeeder.RunAsync(factory.Services, NullLogger.Instance);
        await DevSeeder.RunAsync(factory.Services, NullLogger.Instance);

        await using var scope = factory.CreateSystemScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var owner = await db.Users.SingleAsync(u => u.Email == "demo-owner@contractguard.local", Ct);
        var contracts = await db.Contracts.Where(c => c.OwnerUserAccountId == owner.Id).Select(c => c.ContractId).ToListAsync(Ct);
        Assert.Equal(2, contracts.Count);

        var runs = await db.AnalysisRuns.Where(r => contracts.Contains(r.ContractId)).ToListAsync(Ct);
        Assert.All(runs, r => Assert.Equal(RunStatus.Completed, r.RunStatus));
        Assert.Equal(8, await db.Findings.CountAsync(f => runs.Select(r => r.AnalysisRunId).Contains(f.AnalysisRunId), Ct));
        Assert.Single(await db.ContractShares.Where(s => contracts.Contains(s.ContractId)).ToListAsync(Ct));
        Assert.True(await db.Organizations.AnyAsync(o => o.OrganizationName == "Công ty Demo", Ct));
    }
}
