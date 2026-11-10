using System.Globalization;
using System.Text.Json;
using ContractGuard.Application.Abstractions;
using ContractGuard.Application.Analysis;
using ContractGuard.Domain.Contracts;
using ContractGuard.Domain.Workspaces;
using ContractGuard.Infrastructure.Identity;
using ContractGuard.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ContractGuard.Api.Endpoints;

// Demo data for local testing (Development only): two accounts, an organization, three contracts.
// Results go through the real receiving path (AnalysisResultHandler: schema, consistency, processor),
// with the example worker result, so seeded data is exactly what a worker result would produce.
// Idempotent: does nothing when the demo owner already has contracts.
internal static class DevSeeder
{
    public const string Password = "Demo-mat-khau-1";
    private const string OwnerEmail = "demo-owner@contractguard.local";
    private const string MemberEmail = "demo-member@contractguard.local";

    public static async Task RunAsync(IServiceProvider services, ILogger logger)
    {
        var cancellationToken = CancellationToken.None;
        await using var scope = services.CreateAsyncScope();
        var sp = scope.ServiceProvider;
        sp.GetRequiredService<CurrentWorkspace>().RunAsSystem();
        var db = sp.GetRequiredService<AppDbContext>();
        var users = sp.GetRequiredService<UserManager<AppUser>>();

        var owner = await EnsureUserAsync(users, OwnerEmail, "Chủ hợp đồng Demo");
        var member = await EnsureUserAsync(users, MemberEmail, "Thành viên Demo");
        if (await db.Contracts.AnyAsync(c => c.OwnerUserAccountId == owner.Id, cancellationToken))
        {
            logger.LogInformation("Seed: demo data already present, nothing to do");
            return;
        }

        var now = DateTimeOffset.UtcNow;
        var organization = Organization.Create("Công ty Demo", owner.Id, now);
        organization.AddMember(member.Id, now);
        db.Organizations.Add(organization);
        await db.SaveChangesAsync(cancellationToken);

        await SeedContractAsync(services, "Hợp đồng dịch vụ đám mây (cá nhân)", null, owner.Id, analyze: true, cancellationToken);
        var shared = await SeedContractAsync(services, "Hợp đồng nhà cung cấp SaaS", organization.OrganizationId, owner.Id, analyze: true, cancellationToken);
        await SeedContractAsync(services, "Hợp đồng chờ khai báo ngữ cảnh", organization.OrganizationId, member.Id, analyze: false, cancellationToken);

        db.ContractShares.Add(ContractShare.Create(shared, member.Id, owner.Id, now));
        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Seed: demo accounts {Owner} and {Member} created (password in docs/devlog/Runbook.md)", OwnerEmail, MemberEmail);
    }

    private static async Task<AppUser> EnsureUserAsync(UserManager<AppUser> users, string email, string displayName)
    {
        var user = await users.FindByEmailAsync(email);
        if (user is not null)
        {
            return user;
        }

        user = new AppUser { UserName = email, Email = email, EmailConfirmed = true, DisplayName = displayName };
        var result = await users.CreateAsync(user, Password);
        return result.Succeeded ? user : throw new InvalidOperationException(string.Join("; ", result.Errors.Select(e => e.Description)));
    }

    private static async Task<Guid> SeedContractAsync(IServiceProvider services, string name, Guid? organizationId, Guid ownerId, bool analyze, CancellationToken cancellationToken)
    {
        await using var scope = services.CreateAsyncScope();
        var sp = scope.ServiceProvider;
        sp.GetRequiredService<CurrentWorkspace>().RunAsSystem();
        var db = sp.GetRequiredService<AppDbContext>();
        var storage = sp.GetRequiredService<IObjectStorage>();
        var now = DateTimeOffset.UtcNow;

        var pdf = Resource("sample-contract.pdf");
        var key = AnalysisStorageKeys.QuarantineKey(ownerId, Guid.NewGuid(), ".pdf");
        await storage.PutBytesAsync(key, pdf, "application/pdf", cancellationToken);

        var contract = Contract.Create(name, organizationId, ownerId, new ContractFile(key, "sample-contract.pdf", FileType.Pdf, pdf.Length), now);
        db.Contracts.Add(contract);
        db.AuditEvents.Add(AuditEvent.Create(contract.ContractId, ownerId, AuditAction.Uploaded, "seed", now));
        if (!analyze)
        {
            await db.SaveChangesAsync(cancellationToken);
            return contract.ContractId;
        }

        var version = contract.LatestVersion;
        contract.ConfirmContext(version.ContractVersionId, new ContextDetails(OwnParty.Buyer, ContractType.ServiceAgreement, true, "Example Cloud Pte. Ltd.", "Dịch vụ lưu trữ đám mây", ServiceCriticality.High), now);
        contract.MarkAnalysisStarted(version.ContractVersionId, now);
        var run = Domain.Analysis.AnalysisRun.Start(contract.ContractId, version.ContractVersionId, now);
        db.AnalysisRuns.Add(run);
        await db.SaveChangesAsync(cancellationToken);

        var extraction = System.Text.Encoding.UTF8.GetString(Resource("extraction.v2.example.json"))
            .Replace("{{JOB_ID}}", run.AnalysisRunId.ToString(), StringComparison.Ordinal)
            .Replace("{{TENANT_ID}}", "seed", StringComparison.Ordinal)
            .Replace("{{CONTRACT_ID}}", contract.ContractId.ToString(), StringComparison.Ordinal)
            .Replace("{{CONTRACT_VERSION}}", version.VersionNumber.ToString(CultureInfo.InvariantCulture), StringComparison.Ordinal);
        var resultKey = AnalysisStorageKeys.ExtractionKey(run.AnalysisRunId);
        using (var json = JsonDocument.Parse(extraction))
        {
            await storage.PutJsonAsync(resultKey, json.RootElement, cancellationToken);
        }

        // Same as the SQS consumer: one scope per message.
        await using var handlerScope = services.CreateAsyncScope();
        handlerScope.ServiceProvider.GetRequiredService<CurrentWorkspace>().RunAsSystem();
        var outcome = await handlerScope.ServiceProvider.GetRequiredService<AnalysisResultHandler>()
            .HandleAsync(new AnalysisResultMessage(AnalysisEventTypes.Completed, run.AnalysisRunId, null, resultKey, null, null), cancellationToken);
        return outcome == ResultOutcome.Applied
            ? contract.ContractId
            : throw new InvalidOperationException($"Seed result for '{name}' was {outcome}; see the log for the reason.");
    }

    private static byte[] Resource(string name)
    {
        using var stream = typeof(DevSeeder).Assembly.GetManifestResourceStream(name)
            ?? throw new InvalidOperationException($"Embedded seed file {name} is missing.");
        using var copy = new MemoryStream();
        stream.CopyTo(copy);
        return copy.ToArray();
    }
}
