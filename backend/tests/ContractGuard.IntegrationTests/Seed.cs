using ContractGuard.Domain.Analysis;
using ContractGuard.Domain.Contracts;
using ContractGuard.Infrastructure.Identity;
using ContractGuard.Infrastructure.Persistence;

namespace ContractGuard.IntegrationTests;

internal static class Seed
{
    public static async Task<AppUser> UserAsync(AppDbContext db, string? email = null)
    {
        email ??= $"user-{Guid.NewGuid():N}@example.com";
        var user = new AppUser
        {
            Id = Guid.CreateVersion7(),
            UserName = email,
            NormalizedUserName = email.ToUpperInvariant(),
            Email = email,
            NormalizedEmail = email.ToUpperInvariant(),
            EmailConfirmed = true,
            SecurityStamp = Guid.NewGuid().ToString()
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return user;
    }

    // A personal contract with V1, a confirmed context and a running analysis.
    public static async Task<(Contract Contract, AnalysisRun Run)> RunningJobAsync(AppDbContext db, DateTimeOffset now)
    {
        var user = await UserAsync(db);
        var contract = Contract.Create(
            "Integration test contract",
            null,
            user.Id,
            new ContractFile($"quarantine/{Guid.CreateVersion7()}.pdf", "contract.pdf", FileType.Pdf, 1_000),
            now);
        var version = contract.LatestVersion;
        contract.ConfirmContext(
            version.ContractVersionId,
            new ContextDetails(OwnParty.Buyer, ContractType.ServiceAgreement, true, null, null, null),
            now);
        contract.MarkAnalysisStarted(version.ContractVersionId, now);
        var run = AnalysisRun.Start(contract.ContractId, version.ContractVersionId, now);
        db.Contracts.Add(contract);
        db.AnalysisRuns.Add(run);
        await db.SaveChangesAsync();
        return (contract, run);
    }

    // Path of a file in the repository, found by walking up from the test binaries.
    public static string RepoFile(string relativePath)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !Directory.Exists(Path.Combine(directory.FullName, "contracts")))
        {
            directory = directory.Parent;
        }

        return Path.Combine(directory!.FullName, relativePath);
    }

    public static string ExampleExtractionJson(Guid jobId, Guid contractId, int version = 1)
    {
        return File.ReadAllText(RepoFile("contracts/examples/extraction.v2.example.json"))
            .Replace("{{JOB_ID}}", jobId.ToString(), StringComparison.Ordinal)
            .Replace("{{CONTRACT_ID}}", contractId.ToString(), StringComparison.Ordinal)
            .Replace("{{CONTRACT_VERSION}}", version.ToString(System.Globalization.CultureInfo.InvariantCulture), StringComparison.Ordinal);
    }
}
