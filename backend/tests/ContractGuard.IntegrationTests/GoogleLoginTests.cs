using System.Net;
using ContractGuard.Api.Endpoints;
using ContractGuard.Infrastructure.Identity;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace ContractGuard.IntegrationTests;

// F-AUTH-002, FR-AUTH-011. Google itself cannot run here: the account rules are tested through
// GoogleAccounts, the redirects through the two endpoints.
[Collection(ApiCollection.Name)]
public sealed class GoogleLoginTests(ApiFactory factory)
{
    private const string NewPassword = "Mat-khau-moi-2";

    [Fact]
    public async Task New_account_is_verified_without_password_and_named_from_Google()
    {
        var email = Scenario.NewEmail();
        var user = await FindOrCreateAsync(NewKey(), email, "  Lê Văn C  ");

        await using var scope = factory.Services.CreateAsyncScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        var saved = (await users.FindByIdAsync(user.Id.ToString()))!;
        Assert.True(saved.EmailConfirmed);
        Assert.False(await users.HasPasswordAsync(saved));
        Assert.Equal("Lê Văn C", saved.DisplayName);
        Assert.Equal(email, saved.Email);
    }

    [Fact]
    public async Task Without_a_Google_name_the_part_before_the_at_sign_is_the_name()
    {
        var user = await FindOrCreateAsync(NewKey(), $"tran.b.{Guid.NewGuid():N}@example.com", null);
        Assert.StartsWith("tran.b.", user.DisplayName, StringComparison.Ordinal);
        Assert.DoesNotContain('@', user.DisplayName);
    }

    [Fact]
    public async Task Verified_email_account_is_linked_and_keeps_its_password_and_name()
    {
        var (client, userId, email) = await Scenario.SignedInUserAsync(factory);
        client.Dispose();

        var user = await FindOrCreateAsync(NewKey(), email, "Tên khác từ Google");

        Assert.Equal(userId, user.Id);
        Assert.Equal(Scenario.DisplayName, user.DisplayName);
        using var again = factory.CreateApiClient();
        ApiAssert.Status(await again.PostAsync("/auth/login", new { email, password = Scenario.Password }), HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Unverified_account_loses_the_password_someone_else_may_have_set()
    {
        var email = Scenario.NewEmail();
        using var client = factory.CreateApiClient();
        ApiAssert.Status(await client.PostAsync("/auth/register", new { displayName = Scenario.DisplayName, email, password = Scenario.Password }), HttpStatusCode.NoContent);

        var user = await FindOrCreateAsync(NewKey(), email, null);

        Assert.True(user.EmailConfirmed);
        await ApiAssert.ProblemAsync(
            await client.PostAsync("/auth/login", new { email, password = Scenario.Password }),
            HttpStatusCode.Unauthorized,
            "ERR-AUTH-003");
    }

    [Fact]
    public async Task Next_sign_in_finds_the_account_by_Google_id_even_with_a_new_address()
    {
        var key = NewKey();
        var first = await FindOrCreateAsync(key, Scenario.NewEmail(), "A");
        var second = await FindOrCreateAsync(key, Scenario.NewEmail(), "A");
        Assert.Equal(first.Id, second.Id);
    }

    // FR-AUTH-009: an account made with Google gets a password through "forgot password".
    [Fact]
    public async Task Google_account_sets_a_password_through_forgot_password()
    {
        var email = Scenario.NewEmail();
        await FindOrCreateAsync(NewKey(), email, null);
        using var client = factory.CreateApiClient();

        ApiAssert.Status(await client.PostAsync("/auth/forgot-password", new { email }), HttpStatusCode.NoContent);
        var (userId, token) = factory.Emails.LastLink(email);
        ApiAssert.Status(await client.PostAsync("/auth/reset-password", new { userId, token, newPassword = NewPassword }), HttpStatusCode.NoContent);
        ApiAssert.Status(await client.PostAsync("/auth/login", new { email, password = NewPassword }), HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Without_a_Google_client_the_button_leads_back_to_login()
    {
        using var http = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await http.GetAsync("/api/v1/auth/google?next=/contracts", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("http://localhost:5173/login?google=unavailable", response.Headers.Location!.ToString());
    }

    [Fact]
    public async Task With_a_Google_client_the_browser_goes_to_Google_and_comes_back_through_the_api()
    {
        using var http = factory
            .WithWebHostBuilder(builder =>
            {
                builder.UseSetting("Authentication:Google:ClientId", "test-client-id");
                builder.UseSetting("Authentication:Google:ClientSecret", "test-client-secret");
            })
            .CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await http.GetAsync("/api/v1/auth/google?next=/invitations/abc", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        var location = response.Headers.Location!.ToString();
        Assert.StartsWith("https://accounts.google.com/", location, StringComparison.Ordinal);
        Assert.Contains("client_id=test-client-id", location, StringComparison.Ordinal);
        Assert.Contains(Uri.EscapeDataString("http://localhost/api/v1/auth/google/callback"), location, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Completing_without_a_Google_result_leads_back_to_login()
    {
        using var http = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await http.GetAsync("/api/v1/auth/google/complete?next=/contracts", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("http://localhost:5173/login?google=failed", response.Headers.Location!.ToString());
    }

    [Theory]
    [InlineData("/invitations/abc", "/invitations/abc")]
    [InlineData(null, "/contracts")]
    [InlineData("", "/contracts")]
    [InlineData("https://evil.example", "/contracts")]
    [InlineData("//evil.example", "/contracts")]
    [InlineData("/\\evil.example", "/contracts")]
    public void Next_stays_on_this_site(string? next, string expected) =>
        Assert.Equal(expected, GoogleLoginEndpoints.SafeNext(next));

    private static string NewKey() => Guid.NewGuid().ToString("N");

    private async Task<AppUser> FindOrCreateAsync(string key, string email, string? name)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<GoogleAccounts>().FindOrCreateAsync(key, email, name);
    }
}
