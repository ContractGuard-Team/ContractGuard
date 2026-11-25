using System.Net;
using ContractGuard.Domain.Workspaces;
using ContractGuard.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ContractGuard.IntegrationTests;

// SD-05, CDC-DATA-001: who can see which contract. Each case goes through the real query filters.
[Collection(ApiCollection.Name)]
public sealed class WorkspaceIsolationTests(ApiFactory factory)
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task User_b_cannot_reach_anything_of_user_a()
    {
        var (alice, _, _) = await Scenario.SignedInUserAsync(factory);
        var (bob, _, _) = await Scenario.SignedInUserAsync(factory);
        using (alice)
        using (bob)
        {
            var (contractId, versionId) = await Scenario.UploadContractAsync(factory, alice);
            var jobId = await Scenario.StartAnalysisAsync(alice, contractId, versionId);
            await Scenario.CompleteJobAsync(factory, jobId, contractId);

            Assert.Equal(1, (await ApiAssert.JsonAsync(await alice.GetAsync("/contracts"))).GetProperty("items").GetArrayLength());
            Assert.Equal(0, (await ApiAssert.JsonAsync(await bob.GetAsync("/contracts"))).GetProperty("items").GetArrayLength());

            // 404, not 403: Bob does not even learn that the contract exists.
            await ApiAssert.ProblemAsync(await bob.GetAsync($"/contracts/{contractId}"), HttpStatusCode.NotFound, "ERR-GEN-NOT-FOUND");
            await ApiAssert.ProblemAsync(await bob.GetAsync($"/contracts/{contractId}/versions/{versionId}/file"), HttpStatusCode.NotFound, "ERR-GEN-NOT-FOUND");
            await ApiAssert.ProblemAsync(await bob.GetAsync($"/jobs/{jobId}"), HttpStatusCode.NotFound, "ERR-GEN-NOT-FOUND");
            await ApiAssert.ProblemAsync(await bob.GetAsync($"/jobs/{jobId}/clauses"), HttpStatusCode.NotFound, "ERR-GEN-NOT-FOUND");
            await ApiAssert.ProblemAsync(
                await bob.PutAsync($"/contracts/{contractId}/versions/{versionId}/context", Scenario.Context),
                HttpStatusCode.NotFound,
                "ERR-GEN-NOT-FOUND");
            await ApiAssert.ProblemAsync(
                await bob.PostAsync($"/contracts/{contractId}/versions/{versionId}/analyses"),
                HttpStatusCode.NotFound,
                "ERR-GEN-NOT-FOUND");
        }
    }

    [Fact]
    public async Task A_scope_without_a_user_sees_no_contract_data()
    {
        // Deny by default: a query outside a request, without RunAsSystem(), must find nothing.
        var (client, _, _) = await Scenario.SignedInUserAsync(factory);
        using (client)
        {
            var (contractId, versionId) = await Scenario.UploadContractAsync(factory, client);
            var jobId = await Scenario.StartAnalysisAsync(client, contractId, versionId);
            await Scenario.CompleteJobAsync(factory, jobId, contractId);

            await using var scope = factory.Services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            Assert.False(await db.Contracts.AnyAsync(c => c.ContractId == contractId, Ct));
            Assert.False(await db.ContractVersions.AnyAsync(v => v.ContractVersionId == versionId, Ct));
            Assert.False(await db.AnalysisRuns.AnyAsync(r => r.AnalysisRunId == jobId, Ct));
            Assert.False(await db.Clauses.AnyAsync(c => c.AnalysisRunId == jobId, Ct));

            await using var system = factory.CreateSystemScope();
            Assert.True(await system.ServiceProvider.GetRequiredService<AppDbContext>().Clauses.AnyAsync(c => c.AnalysisRunId == jobId, Ct));
        }
    }

    [Fact]
    public async Task Stranger_cannot_enter_an_organization()
    {
        var (owner, ownerId, _) = await Scenario.SignedInUserAsync(factory);
        var (stranger, _, _) = await Scenario.SignedInUserAsync(factory);
        using (owner)
        using (stranger)
        {
            var organizationId = await CreateOrganizationAsync(ownerId);

            await ApiAssert.ProblemAsync(await stranger.GetAsync("/contracts", organizationId), HttpStatusCode.Forbidden, "ERR-SEC-001");
        }
    }

    [Fact]
    public async Task Workspace_header_must_be_a_uuid()
    {
        var (client, _, _) = await Scenario.SignedInUserAsync(factory);
        using (client)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/contracts");
            request.Headers.Add("X-Workspace-Id", "not-a-uuid");

            await ApiAssert.ProblemAsync(await client.Http.SendAsync(request, Ct), HttpStatusCode.BadRequest, "ERR-GEN-VALIDATION");
        }
    }

    [Fact]
    public async Task In_an_organization_a_member_sees_own_contracts_and_an_owner_sees_all()
    {
        // BR-SHARE-001, SEC-AUTHZ-005
        var (owner, ownerId, _) = await Scenario.SignedInUserAsync(factory);
        var (member, memberId, _) = await Scenario.SignedInUserAsync(factory);
        var (otherMember, otherMemberId, _) = await Scenario.SignedInUserAsync(factory);
        using (owner)
        using (member)
        using (otherMember)
        {
            var organizationId = await CreateOrganizationAsync(ownerId);
            await AddMemberAsync(organizationId, memberId);
            await AddMemberAsync(organizationId, otherMemberId);

            var (contractId, versionId) = await Scenario.UploadContractAsync(factory, member, organizationId);

            await using (var scope = factory.CreateSystemScope())
            {
                var saved = await scope.ServiceProvider.GetRequiredService<AppDbContext>().Contracts.SingleAsync(c => c.ContractId == contractId, Ct);
                Assert.Equal(organizationId, saved.OrganizationId);
            }

            // The member's own contract, in the organization and not in the personal workspace.
            Assert.Equal(1, (await ApiAssert.JsonAsync(await member.GetAsync("/contracts", organizationId))).GetProperty("items").GetArrayLength());
            Assert.Equal(0, (await ApiAssert.JsonAsync(await member.GetAsync("/contracts"))).GetProperty("items").GetArrayLength());

            // Another member has no access without a share.
            Assert.Equal(0, (await ApiAssert.JsonAsync(await otherMember.GetAsync("/contracts", organizationId))).GetProperty("items").GetArrayLength());
            await ApiAssert.ProblemAsync(await otherMember.GetAsync($"/contracts/{contractId}", organizationId), HttpStatusCode.NotFound, "ERR-GEN-NOT-FOUND");
            await ApiAssert.ProblemAsync(await otherMember.GetAsync($"/contracts/{contractId}/versions/{versionId}/file", organizationId), HttpStatusCode.NotFound, "ERR-GEN-NOT-FOUND");

            // The organization owner has every right of a contract owner.
            Assert.Equal(1, (await ApiAssert.JsonAsync(await owner.GetAsync("/contracts", organizationId))).GetProperty("items").GetArrayLength());
            await Scenario.StartAnalysisAsync(owner, contractId, versionId, organizationId);

            // Nothing leaks into the owner's personal workspace, and the role does not carry over there.
            Assert.Equal(0, (await ApiAssert.JsonAsync(await owner.GetAsync("/contracts"))).GetProperty("items").GetArrayLength());
            await ApiAssert.ProblemAsync(await owner.GetAsync($"/contracts/{contractId}"), HttpStatusCode.NotFound, "ERR-GEN-NOT-FOUND");

            var me = await ApiAssert.JsonAsync(await member.GetAsync("/me"));
            Assert.Equal(2, me.GetProperty("workspaces").GetArrayLength());
            Assert.Equal("member", me.GetProperty("workspaces")[1].GetProperty("role").GetString());
        }
    }

    [Fact]
    public async Task One_upload_cannot_become_contracts_in_two_workspaces()
    {
        var (owner, ownerId, _) = await Scenario.SignedInUserAsync(factory);
        using (owner)
        {
            var organizationId = await CreateOrganizationAsync(ownerId);
            var uploadId = await Scenario.UploadFileAsync(factory, owner, "a.pdf", Scenario.PdfBytes);
            ApiAssert.Status(await owner.PostAsync("/contracts", new { uploadId, fileName = "a.pdf" }), HttpStatusCode.OK);

            await ApiAssert.ProblemAsync(
                await owner.PostAsync("/contracts", new { uploadId, fileName = "a.pdf" }, organizationId),
                HttpStatusCode.BadRequest,
                "ERR-GEN-VALIDATION");
        }
    }

    [Fact]
    public async Task Removing_a_member_takes_effect_on_the_next_request()
    {
        // SEC-AUTHZ-008
        var (owner, ownerId, _) = await Scenario.SignedInUserAsync(factory);
        var (member, memberId, _) = await Scenario.SignedInUserAsync(factory);
        using (owner)
        using (member)
        {
            var organizationId = await CreateOrganizationAsync(ownerId);
            await AddMemberAsync(organizationId, memberId);
            ApiAssert.Status(await member.GetAsync("/contracts", organizationId), HttpStatusCode.OK);

            await using (var scope = factory.Services.CreateAsyncScope())
            {
                await scope.ServiceProvider.GetRequiredService<AppDbContext>().Memberships
                    .Where(m => m.OrganizationId == organizationId && m.UserAccountId == memberId)
                    .ExecuteDeleteAsync(Ct);
            }

            await ApiAssert.ProblemAsync(await member.GetAsync("/contracts", organizationId), HttpStatusCode.Forbidden, "ERR-SEC-001");
        }
    }

    private async Task<Guid> CreateOrganizationAsync(Guid ownerId)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var organization = Organization.Create("Công ty thử nghiệm", ownerId, DateTimeOffset.UtcNow);
        db.Organizations.Add(organization);
        await db.SaveChangesAsync(Ct);
        return organization.OrganizationId;
    }

    // SQL because adding members to an organization is a Phase 5 feature (P5-01).
    private async Task AddMemberAsync(Guid organizationId, Guid userId)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.ExecuteSqlInterpolatedAsync(
            $"INSERT INTO app.membership (membership_id, organization_id, user_account_id, org_role, joined_at) VALUES ({Guid.CreateVersion7()}, {organizationId}, {userId}, 'member', now())",
            Ct);
    }
}
