using System.Net;
using ContractGuard.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ContractGuard.IntegrationTests;

// Phase 5 against the access control matrix of SRS §12.3: organizations, sharing, library, versions,
// deletion, activity log. Everything through the API, as the frontend does it.
[Collection(ApiCollection.Name)]
public sealed class ManagementTests(ApiFactory factory)
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Organization_invitation_roles_and_removal()
    {
        var (owner, _, _) = await Scenario.SignedInUserAsync(factory);
        var (member, memberId, memberEmail) = await Scenario.SignedInUserAsync(factory);
        var (stranger, _, _) = await Scenario.SignedInUserAsync(factory);
        using (owner)
        using (member)
        using (stranger)
        {
            var organizationId = (await ApiAssert.JsonAsync(await owner.PostAsync("/organizations", new { name = "Công ty An Bình" }))).GetProperty("organizationId").GetGuid();

            ApiAssert.Status(await owner.PostAsync($"/organizations/{organizationId}/invitations", new { email = memberEmail.ToUpperInvariant() }), HttpStatusCode.NoContent);
            var token = factory.Emails.LastInvitationToken(memberEmail);

            // FR-ORG-003: the link page works without a session and names the organization.
            using (var anonymous = factory.CreateApiClient())
            {
                var view = await ApiAssert.JsonAsync(await anonymous.GetAsync($"/invitations/{token}"));
                Assert.Equal("Công ty An Bình", view.GetProperty("organizationName").GetString());
            }

            // Only the invited address can accept a forwarded link.
            await ApiAssert.ProblemAsync(await stranger.PostAsync($"/invitations/{token}/accept"), HttpStatusCode.Forbidden, "ERR-SEC-001");
            ApiAssert.Status(await member.PostAsync($"/invitations/{token}/accept"), HttpStatusCode.OK);

            // TR-ORG-001: the invitation is gone once used (FR-ORG-005 on reuse).
            await ApiAssert.ProblemAsync(await member.PostAsync($"/invitations/{token}/accept"), HttpStatusCode.BadRequest, "ERR-ORG-002");

            // FR-ORG-006: members see every member with email and role; strangers learn nothing.
            var detail = await ApiAssert.JsonAsync(await member.GetAsync($"/organizations/{organizationId}"));
            Assert.Equal(2, detail.GetProperty("members").GetArrayLength());
            Assert.Equal("member", detail.GetProperty("myRole").GetString());
            await ApiAssert.ProblemAsync(await stranger.GetAsync($"/organizations/{organizationId}"), HttpStatusCode.NotFound, "ERR-GEN-NOT-FOUND");
            await ApiAssert.ProblemAsync(await member.PostAsync($"/organizations/{organizationId}/invitations", new { email = "x@example.com" }), HttpStatusCode.Forbidden, "ERR-SEC-001");

            // FR-ORG-010: the last owner cannot step down.
            var ownerId = detail.GetProperty("members")[0].GetProperty("userId").GetGuid();
            await ApiAssert.ProblemAsync(await owner.PutAsync($"/organizations/{organizationId}/members/{ownerId}/role", new { role = "member" }), HttpStatusCode.BadRequest, "ERR-ORG-003");

            // FR-ORG-011: the member's contract moves to the owner who removes them; their access ends at once.
            var (contractId, _) = await Scenario.UploadContractAsync(factory, member, organizationId);
            ApiAssert.Status(await owner.DeleteAsync($"/organizations/{organizationId}/members/{memberId}"), HttpStatusCode.NoContent);
            await ApiAssert.ProblemAsync(await member.GetAsync("/contracts", organizationId), HttpStatusCode.Forbidden, "ERR-SEC-001");
            var moved = await ApiAssert.JsonAsync(await owner.GetAsync($"/contracts/{contractId}", organizationId));
            Assert.Equal("owner", moved.GetProperty("myRole").GetString());
            var library = await ApiAssert.JsonAsync(await owner.GetAsync("/contracts", organizationId));
            Assert.False(library.GetProperty("items")[0].GetProperty("sharedWithMe").GetBoolean());
        }
    }

    [Fact]
    public async Task Expired_invitation_is_refused_and_deleted()
    {
        var (owner, _, _) = await Scenario.SignedInUserAsync(factory);
        var (invitee, _, email) = await Scenario.SignedInUserAsync(factory);
        using (owner)
        using (invitee)
        {
            var organizationId = (await ApiAssert.JsonAsync(await owner.PostAsync("/organizations", new { name = "Org" }))).GetProperty("organizationId").GetGuid();
            ApiAssert.Status(await owner.PostAsync($"/organizations/{organizationId}/invitations", new { email }), HttpStatusCode.NoContent);
            await using (var scope = factory.Services.CreateAsyncScope())
            {
                await scope.ServiceProvider.GetRequiredService<AppDbContext>().Invitations
                    .Where(i => i.OrganizationId == organizationId)
                    .ExecuteUpdateAsync(u => u.SetProperty(i => i.ExpiresAt, DateTimeOffset.UtcNow.AddMinutes(-1)), Ct);
            }

            await ApiAssert.ProblemAsync(await invitee.PostAsync($"/invitations/{factory.Emails.LastInvitationToken(email)}/accept"), HttpStatusCode.BadRequest, "ERR-ORG-002");
            await using var check = factory.Services.CreateAsyncScope();
            Assert.False(await check.ServiceProvider.GetRequiredService<AppDbContext>().Invitations.AnyAsync(i => i.OrganizationId == organizationId, Ct));
        }
    }

    [Fact]
    public async Task Sharing_follows_the_contract_role_matrix()
    {
        var (owner, _, _) = await Scenario.SignedInUserAsync(factory);
        var (member, memberId, memberEmail) = await Scenario.SignedInUserAsync(factory);
        var (outsider, _, outsiderEmail) = await Scenario.SignedInUserAsync(factory);
        using (owner)
        using (member)
        using (outsider)
        {
            var organizationId = await OrganizationWithMemberAsync(owner, member, memberEmail);
            var (contractId, versionId, jobId) = await Scenario.AnalyzedContractAsync(factory, owner, organizationId);

            // BR-SHARE-001: a member does not see it until it is shared.
            await ApiAssert.ProblemAsync(await member.GetAsync($"/contracts/{contractId}", organizationId), HttpStatusCode.NotFound, "ERR-GEN-NOT-FOUND");

            // FR-SHARE-004: organization contracts stay inside the organization.
            await ApiAssert.ProblemAsync(await owner.PostAsync($"/contracts/{contractId}/shares", new { email = outsiderEmail }, organizationId), HttpStatusCode.BadRequest, "ERR-SHARE-001");

            ApiAssert.Status(await owner.PostAsync($"/contracts/{contractId}/shares", new { email = memberEmail }, organizationId), HttpStatusCode.NoContent);
            var shares = await ApiAssert.JsonAsync(await owner.GetAsync($"/contracts/{contractId}/shares", organizationId));
            Assert.Equal(memberEmail, shares.GetProperty("shares")[0].GetProperty("email").GetString());

            // Member of the contract: reads, edits, exports...
            var seen = await ApiAssert.JsonAsync(await member.GetAsync($"/contracts/{contractId}", organizationId));
            Assert.Equal("member", seen.GetProperty("myRole").GetString());
            ApiAssert.Status(await member.PutAsync($"/contracts/{contractId}/versions/{versionId}/metadata", new { governingLaw = "Việt Nam" }, organizationId), HttpStatusCode.NoContent);
            ApiAssert.Status(await member.GetAsync($"/jobs/{jobId}/report?format=docx", organizationId), HttpStatusCode.OK);
            var library = await ApiAssert.JsonAsync(await member.GetAsync("/contracts", organizationId));
            Assert.True(library.GetProperty("items")[0].GetProperty("sharedWithMe").GetBoolean());

            // ...but cannot share, read the log or delete (SRS §12.3).
            await ApiAssert.ProblemAsync(await member.PostAsync($"/contracts/{contractId}/shares", new { email = memberEmail }, organizationId), HttpStatusCode.Forbidden, "ERR-SEC-001");
            await ApiAssert.ProblemAsync(await member.GetAsync($"/contracts/{contractId}/audit", organizationId), HttpStatusCode.Forbidden, "ERR-SEC-001");
            await ApiAssert.ProblemAsync(await member.DeleteAsync($"/contracts/{contractId}", organizationId), HttpStatusCode.Forbidden, "ERR-SEC-001");

            // FR-SHARE-007: revocation applies to the very next request.
            ApiAssert.Status(await owner.DeleteAsync($"/contracts/{contractId}/shares/{memberId}", organizationId), HttpStatusCode.NoContent);
            await ApiAssert.ProblemAsync(await member.GetAsync($"/contracts/{contractId}", organizationId), HttpStatusCode.NotFound, "ERR-GEN-NOT-FOUND");

            // FR-SHARE-008: the new owner takes over, the previous owner stays as a member.
            ApiAssert.Status(await owner.PutAsync($"/contracts/{contractId}/owner", new { userId = memberId }, organizationId), HttpStatusCode.NoContent);
            Assert.Equal("owner", (await ApiAssert.JsonAsync(await member.GetAsync($"/contracts/{contractId}", organizationId))).GetProperty("myRole").GetString());

            // FR-AUD-001, FR-AUD-002: the log has who did what.
            var log = await ApiAssert.JsonAsync(await member.GetAsync($"/contracts/{contractId}/audit", organizationId));
            var actions = log.EnumerateArray().Select(e => e.GetProperty("action").GetString()).ToList();
            Assert.Contains("uploaded", actions);
            Assert.Contains("context_confirmed", actions);
            Assert.Contains("viewed", actions);
            Assert.Contains("shared", actions);
            Assert.Contains("metadata_edited", actions);
            Assert.Contains("report_exported", actions);
            Assert.Contains("share_revoked", actions);
            Assert.Contains("owner_transferred", actions);
        }
    }

    [Fact]
    public async Task Personal_contracts_are_shared_with_registered_accounts_only()
    {
        var (owner, _, _) = await Scenario.SignedInUserAsync(factory);
        var (friend, _, friendEmail) = await Scenario.SignedInUserAsync(factory);
        using (owner)
        using (friend)
        {
            var (contractId, _) = await Scenario.UploadContractAsync(factory, owner);

            await ApiAssert.ProblemAsync(await owner.PostAsync($"/contracts/{contractId}/shares", new { email = "nobody-here@example.com" }), HttpStatusCode.BadRequest, "ERR-SHARE-002");
            ApiAssert.Status(await owner.PostAsync($"/contracts/{contractId}/shares", new { email = friendEmail }), HttpStatusCode.NoContent);

            // FR-CTR-008: it shows in the friend's personal workspace.
            var library = await ApiAssert.JsonAsync(await friend.GetAsync("/contracts"));
            Assert.Equal(contractId, library.GetProperty("items")[0].GetProperty("contractId").GetGuid());

            // A personal contract keeps its owner (§12.3).
            await ApiAssert.ProblemAsync(await owner.PutAsync($"/contracts/{contractId}/owner", new { userId = Guid.NewGuid() }), HttpStatusCode.BadRequest, "ERR-GEN-VALIDATION");
        }
    }

    [Fact]
    public async Task Library_searches_filters_sorts_and_pages()
    {
        var (client, _, _) = await Scenario.SignedInUserAsync(factory);
        using (client)
        {
            var (analyzed, _, _) = await Scenario.AnalyzedContractAsync(factory, client);
            await Scenario.UploadContractAsync(factory, client);
            await Scenario.UploadContractAsync(factory, client);

            var all = await ApiAssert.JsonAsync(await client.GetAsync("/contracts?pageSize=2"));
            Assert.Equal(3, all.GetProperty("total").GetInt32());
            Assert.Equal(2, all.GetProperty("items").GetArrayLength());

            // FR-CTR-009 by counterparty name of the context (Scenario.Context has none) and by contract name.
            Assert.Equal(3, (await ApiAssert.JsonAsync(await client.GetAsync("/contracts?search=HOP-DONG"))).GetProperty("total").GetInt32());
            Assert.Equal(0, (await ApiAssert.JsonAsync(await client.GetAsync("/contracts?search=zzz"))).GetProperty("total").GetInt32());

            // FR-CTR-010: risk level of the latest analysis, approval status.
            var high = await ApiAssert.JsonAsync(await client.GetAsync("/contracts?riskLevel=high"));
            Assert.Equal(analyzed, high.GetProperty("items")[0].GetProperty("contractId").GetGuid());
            Assert.Equal(72, high.GetProperty("items")[0].GetProperty("overallScore").GetInt32());
            Assert.Equal(2, (await ApiAssert.JsonAsync(await client.GetAsync("/contracts?status=new"))).GetProperty("total").GetInt32());

            // FR-CTR-011: by score, highest first, unscored last.
            var byScore = await ApiAssert.JsonAsync(await client.GetAsync("/contracts?sort=score&descending=true"));
            Assert.Equal(analyzed, byScore.GetProperty("items")[0].GetProperty("contractId").GetGuid());

            await ApiAssert.ProblemAsync(await client.GetAsync("/contracts?sort=size"), HttpStatusCode.BadRequest, "ERR-GEN-VALIDATION");
        }
    }

    [Fact]
    public async Task New_version_keeps_the_old_result_and_suggests_the_old_context()
    {
        var (client, _, _) = await Scenario.SignedInUserAsync(factory);
        using (client)
        {
            var (contractId, v1, _) = await Scenario.AnalyzedContractAsync(factory, client);
            var uploadId = await Scenario.UploadFileAsync(factory, client, "hop-dong-v2.pdf", Scenario.PdfBytes);

            var created = await ApiAssert.JsonAsync(await client.PostAsync($"/contracts/{contractId}/versions", new { uploadId, fileName = "hop-dong-v2.pdf" }));
            var v2 = created.GetProperty("contractVersionId").GetGuid();

            // FR-CTR-014, FR-CTR-015, FR-CTR-016
            var latest = await ApiAssert.JsonAsync(await client.GetAsync($"/contracts/{contractId}"));
            Assert.Equal(2, latest.GetProperty("version").GetProperty("versionNumber").GetInt32());
            Assert.Equal("buyer", latest.GetProperty("suggestedContext").GetProperty("ownParty").GetString());
            Assert.Equal(2, latest.GetProperty("versions").GetArrayLength());
            Assert.Equal(JsonValueKindNull, latest.GetProperty("completedJobId").ValueKind);

            // FR-CTR-017: V1 still opens with its own result.
            var old = await ApiAssert.JsonAsync(await client.GetAsync($"/contracts/{contractId}?version={v1}"));
            Assert.False(old.GetProperty("isLatestVersion").GetBoolean());
            Assert.NotEqual(JsonValueKindNull, old.GetProperty("completedJobId").ValueKind);

            // ERR-CTR-003: no new version while one is being analyzed.
            await Scenario.StartAnalysisAsync(client, contractId, v2);
            var another = await Scenario.UploadFileAsync(factory, client, "hop-dong-v3.pdf", Scenario.PdfBytes);
            await ApiAssert.ProblemAsync(await client.PostAsync($"/contracts/{contractId}/versions", new { uploadId = another, fileName = "hop-dong-v3.pdf" }), HttpStatusCode.Conflict, "ERR-CTR-003");
        }
    }

    [Fact]
    public async Task Deleting_a_contract_removes_its_files_and_every_row()
    {
        var (client, _, _) = await Scenario.SignedInUserAsync(factory);
        using (client)
        {
            var (contractId, _, jobId) = await Scenario.AnalyzedContractAsync(factory, client);
            string sourceKey;
            await using (var scope = factory.CreateSystemScope())
            {
                sourceKey = await scope.ServiceProvider.GetRequiredService<AppDbContext>().ContractVersions.Where(v => v.ContractId == contractId).Select(v => v.SourceFileKey).SingleAsync(Ct);
            }

            Assert.True(factory.Storage.Exists(sourceKey));
            Assert.True(factory.Storage.Exists($"results/{jobId}/extraction.json"));

            ApiAssert.Status(await client.DeleteAsync($"/contracts/{contractId}"), HttpStatusCode.NoContent);

            Assert.False(factory.Storage.Exists(sourceKey));
            Assert.False(factory.Storage.Exists($"results/{jobId}/extraction.json"));
            Assert.False(factory.Storage.Exists($"results/{jobId}/request.json"));
            await using var check = factory.CreateSystemScope();
            var db = check.ServiceProvider.GetRequiredService<AppDbContext>();
            Assert.False(await db.Contracts.AnyAsync(c => c.ContractId == contractId, Ct));
            Assert.False(await db.Findings.AnyAsync(f => f.AnalysisRunId == jobId, Ct));
            Assert.False(await db.AuditEvents.IgnoreQueryFilters().AnyAsync(e => e.ContractId == contractId, Ct));
            await ApiAssert.ProblemAsync(await client.GetAsync($"/contracts/{contractId}"), HttpStatusCode.NotFound, "ERR-GEN-NOT-FOUND");
        }
    }

    private const System.Text.Json.JsonValueKind JsonValueKindNull = System.Text.Json.JsonValueKind.Null;

    private async Task<Guid> OrganizationWithMemberAsync(ApiClient owner, ApiClient member, string memberEmail)
    {
        var organizationId = (await ApiAssert.JsonAsync(await owner.PostAsync("/organizations", new { name = "Org" }))).GetProperty("organizationId").GetGuid();
        ApiAssert.Status(await owner.PostAsync($"/organizations/{organizationId}/invitations", new { email = memberEmail }), HttpStatusCode.NoContent);
        ApiAssert.Status(await member.PostAsync($"/invitations/{factory.Emails.LastInvitationToken(memberEmail)}/accept"), HttpStatusCode.OK);
        return organizationId;
    }
}
