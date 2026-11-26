using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
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
// The SaaS contract has two analysed versions, V1 better than V2, for the version comparison (F-CMP-001).
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
        var shared = await SeedContractAsync(services, "Hợp đồng nhà cung cấp SaaS", organization.OrganizationId, owner.Id, analyze: true, cancellationToken, BetterFirstVersion);
        await SeedNextVersionAsync(services, shared, owner.Id, cancellationToken);
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

    private static async Task<Guid> SeedContractAsync(
        IServiceProvider services, string name, Guid? organizationId, Guid ownerId, bool analyze, CancellationToken cancellationToken, Action<JsonNode>? adjust = null)
    {
        await using var scope = services.CreateAsyncScope();
        var sp = scope.ServiceProvider;
        sp.GetRequiredService<CurrentWorkspace>().RunAsSystem();
        var db = sp.GetRequiredService<AppDbContext>();
        var now = DateTimeOffset.UtcNow;

        var contract = Contract.Create(name, organizationId, ownerId, await PutSampleFileAsync(sp, ownerId, cancellationToken), now);
        db.Contracts.Add(contract);
        db.AuditEvents.Add(AuditEvent.Create(contract.ContractId, ownerId, AuditAction.Uploaded, "seed", now));
        await db.SaveChangesAsync(cancellationToken);
        if (analyze)
        {
            await AnalyzeAsync(services, contract.ContractId, adjust, cancellationToken);
        }

        return contract.ContractId;
    }

    // F-CTR-005: the same file again as V2, analysed with the example result as it stands.
    private static async Task SeedNextVersionAsync(IServiceProvider services, Guid contractId, Guid ownerId, CancellationToken cancellationToken)
    {
        await using (var scope = services.CreateAsyncScope())
        {
            var sp = scope.ServiceProvider;
            sp.GetRequiredService<CurrentWorkspace>().RunAsSystem();
            var db = sp.GetRequiredService<AppDbContext>();
            var contract = await db.Contracts.Include(c => c.Versions).SingleAsync(c => c.ContractId == contractId, cancellationToken);
            contract.AddVersion(await PutSampleFileAsync(sp, ownerId, cancellationToken), ownerId, DateTimeOffset.UtcNow);
            await db.SaveChangesAsync(cancellationToken);
        }

        await AnalyzeAsync(services, contractId, null, cancellationToken);
    }

    // V1 of the SaaS contract: two checks pass and the score is lower, so V2 (the example) reads as weaker.
    private static void BetterFirstVersion(JsonNode extraction)
    {
        foreach (var check in extraction["checks"]!.AsArray())
        {
            if (check!["check_code"]!.GetValue<string>() is "CY-01" or "TP-01")
            {
                check["check_status"] = "pass";
                check["note"] = null;
            }
        }

        var scoring = extraction["scoring"]!;
        scoring["overall_score"] = 58;
        foreach (var domain in scoring["domains"]!.AsArray())
        {
            if (domain!["domain"]!.GetValue<string>() == "cybersecurity")
            {
                domain["score"] = 70;
                domain["risk_level"] = "high";
            }
        }
    }

    private static async Task<ContractFile> PutSampleFileAsync(IServiceProvider sp, Guid ownerId, CancellationToken cancellationToken)
    {
        var pdf = Resource("sample-contract.pdf");
        var key = AnalysisStorageKeys.QuarantineKey(ownerId, Guid.NewGuid(), ".pdf");
        await sp.GetRequiredService<IObjectStorage>().PutBytesAsync(key, pdf, "application/pdf", cancellationToken);
        return new ContractFile(key, "sample-contract.pdf", FileType.Pdf, pdf.Length);
    }

    // Analyses the latest version through the real receiving path, with the example result (adjusted if asked).
    private static async Task AnalyzeAsync(IServiceProvider services, Guid contractId, Action<JsonNode>? adjust, CancellationToken cancellationToken)
    {
        await using var scope = services.CreateAsyncScope();
        var sp = scope.ServiceProvider;
        sp.GetRequiredService<CurrentWorkspace>().RunAsSystem();
        var db = sp.GetRequiredService<AppDbContext>();
        var storage = sp.GetRequiredService<IObjectStorage>();
        var now = DateTimeOffset.UtcNow;
        var contract = await db.Contracts.Include(c => c.Versions).ThenInclude(v => v.Context).SingleAsync(c => c.ContractId == contractId, cancellationToken);
        var name = contract.ContractName;

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
        var node = JsonNode.Parse(extraction)!;
        adjust?.Invoke(node);
        using (var json = JsonDocument.Parse(node.ToJsonString()))
        {
            await storage.PutJsonAsync(resultKey, json.RootElement, cancellationToken);
        }

        // Same as the SQS consumer: one scope per message.
        await using var handlerScope = services.CreateAsyncScope();
        handlerScope.ServiceProvider.GetRequiredService<CurrentWorkspace>().RunAsSystem();
        var outcome = await handlerScope.ServiceProvider.GetRequiredService<AnalysisResultHandler>()
            .HandleAsync(new AnalysisResultMessage(AnalysisEventTypes.Completed, run.AnalysisRunId, null, resultKey, null, null), cancellationToken);
        if (outcome != ResultOutcome.Applied)
        {
            throw new InvalidOperationException($"Seed result for '{name}' was {outcome}; see the log for the reason.");
        }
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
