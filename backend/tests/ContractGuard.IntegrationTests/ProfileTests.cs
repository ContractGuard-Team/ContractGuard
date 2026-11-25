using System.Net;
using System.Text.Json;
using ContractGuard.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace ContractGuard.IntegrationTests;

// FR-AUTH-016, FR-AUTH-017 (CR-AUTH-001): the user's own profile and password.
[Collection(ApiCollection.Name)]
public sealed class ProfileTests(ApiFactory factory)
{
    private const string NewPassword = "Mat-khau-moi-7";

    [Fact]
    public async Task Profile_changes_show_in_me_and_an_empty_phone_removes_it()
    {
        var (client, _, email) = await Scenario.SignedInUserAsync(factory);
        using (client)
        {
            var before = await ApiAssert.JsonAsync(await client.GetAsync("/me"));
            Assert.Equal(JsonValueKind.Null, before.GetProperty("phoneNumber").ValueKind);
            Assert.True(before.GetProperty("hasPassword").GetBoolean());

            ApiAssert.Status(
                await client.PutAsync("/me/profile", new { displayName = "  Phạm Văn D  ", phoneNumber = " 0912.345 678 ", uiLanguage = "en" }),
                HttpStatusCode.NoContent);
            var me = await ApiAssert.JsonAsync(await client.GetAsync("/me"));
            Assert.Equal("Phạm Văn D", me.GetProperty("displayName").GetString());
            Assert.Equal("0912345678", me.GetProperty("phoneNumber").GetString());
            Assert.Equal("en", me.GetProperty("uiLanguage").GetString());
            Assert.Equal(email, me.GetProperty("email").GetString());

            ApiAssert.Status(await client.PutAsync("/me/profile", new { displayName = "Phạm Văn D", phoneNumber = "", uiLanguage = "vi" }), HttpStatusCode.NoContent);
            var cleared = await ApiAssert.JsonAsync(await client.GetAsync("/me"));
            Assert.Equal(JsonValueKind.Null, cleared.GetProperty("phoneNumber").ValueKind);
        }
    }

    [Theory]
    [InlineData("   ", null)]
    [InlineData("Tên", "12345")]
    [InlineData("Tên", "0912abc678")]
    [InlineData("Tên", "+1234567890123456")]
    public async Task Invalid_name_or_phone_is_refused_and_nothing_changes(string displayName, string? phoneNumber)
    {
        var (client, _, _) = await Scenario.SignedInUserAsync(factory);
        using (client)
        {
            await ApiAssert.ProblemAsync(
                await client.PutAsync("/me/profile", new { displayName, phoneNumber, uiLanguage = "vi" }),
                HttpStatusCode.BadRequest,
                "ERR-GEN-VALIDATION");
            var me = await ApiAssert.JsonAsync(await client.GetAsync("/me"));
            Assert.Equal(Scenario.DisplayName, me.GetProperty("displayName").GetString());
        }
    }

    [Fact]
    public async Task Password_change_needs_the_current_password_and_then_only_the_new_one_works()
    {
        var (client, _, email) = await Scenario.SignedInUserAsync(factory);
        using (client)
        {
            await ApiAssert.ProblemAsync(
                await client.PostAsync("/me/password", new { currentPassword = "Sai-mat-khau-1", newPassword = NewPassword }),
                HttpStatusCode.BadRequest,
                "ERR-AUTH-007");
            await ApiAssert.ProblemAsync(
                await client.PostAsync("/me/password", new { currentPassword = Scenario.Password, newPassword = "short" }),
                HttpStatusCode.BadRequest,
                "ERR-AUTH-002");

            ApiAssert.Status(await client.PostAsync("/me/password", new { currentPassword = Scenario.Password, newPassword = NewPassword }), HttpStatusCode.NoContent);
            ApiAssert.Status(await client.GetAsync("/me"), HttpStatusCode.OK); // this session was renewed
        }

        using var other = factory.CreateApiClient();
        await ApiAssert.ProblemAsync(await other.PostAsync("/auth/login", new { email, password = Scenario.Password }), HttpStatusCode.Unauthorized, "ERR-AUTH-003");
        ApiAssert.Status(await other.PostAsync("/auth/login", new { email, password = NewPassword }), HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task An_account_without_a_password_sets_one_without_a_current_password()
    {
        var (client, userId, email) = await Scenario.SignedInUserAsync(factory);
        using (client)
        {
            // Stands in for a Google-only account (GoogleLoginTests): the session stays valid until its next stamp check.
            await using (var scope = factory.Services.CreateAsyncScope())
            {
                var users = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
                Assert.True((await users.RemovePasswordAsync((await users.FindByIdAsync(userId.ToString()))!)).Succeeded);
            }

            Assert.False((await ApiAssert.JsonAsync(await client.GetAsync("/me"))).GetProperty("hasPassword").GetBoolean());
            // The API wants every field present (nullable ones as null), like registration.
            ApiAssert.Status(await client.PostAsync("/me/password", new { currentPassword = (string?)null, newPassword = NewPassword }), HttpStatusCode.NoContent);
        }

        using var other = factory.CreateApiClient();
        ApiAssert.Status(await other.PostAsync("/auth/login", new { email, password = NewPassword }), HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task The_chosen_language_is_what_the_worker_is_asked_to_answer_in()
    {
        var (client, _, _) = await Scenario.SignedInUserAsync(factory);
        using (client)
        {
            ApiAssert.Status(await client.PutAsync("/me/profile", new { displayName = Scenario.DisplayName, phoneNumber = (string?)null, uiLanguage = "en" }), HttpStatusCode.NoContent);
            var (contractId, versionId) = await Scenario.UploadContractAsync(factory, client);
            var jobId = await Scenario.StartAnalysisAsync(client, contractId, versionId);

            using var request = JsonDocument.Parse(factory.Storage.GetText($"results/{jobId}/request.json"));
            Assert.Equal("en", request.RootElement.GetProperty("output_language").GetString());
        }
    }
}
