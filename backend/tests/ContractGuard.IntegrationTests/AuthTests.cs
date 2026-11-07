using System.Net;
using ContractGuard.Infrastructure.Identity;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace ContractGuard.IntegrationTests;

// F-AUTH-001 through the real endpoints, Identity and PostgreSQL.
[Collection(ApiCollection.Name)]
public sealed class AuthTests(ApiFactory factory)
{
    [Fact]
    public async Task Full_flow_register_verify_login_logout()
    {
        var email = Scenario.NewEmail();
        using var client = factory.CreateApiClient();

        ApiAssert.Status(await client.PostAsync("/auth/register", new { displayName = Scenario.DisplayName, email, password = Scenario.Password }), HttpStatusCode.NoContent);

        // FR-AUTH-008: no login before the email is verified.
        await ApiAssert.ProblemAsync(
            await client.PostAsync("/auth/login", new { email, password = Scenario.Password }),
            HttpStatusCode.Forbidden,
            "ERR-GEN-EMAIL-UNCONFIRMED");

        var (userId, token) = factory.Emails.LastLink(email);
        ApiAssert.Status(await client.PostAsync("/auth/verify-email", new { userId, token }), HttpStatusCode.NoContent);
        ApiAssert.Status(await client.PostAsync("/auth/login", new { email, password = Scenario.Password }), HttpStatusCode.NoContent);

        // FR-ORG-013: the personal workspace is there from the start.
        var me = await ApiAssert.JsonAsync(await client.GetAsync("/me"));
        Assert.Equal(email, me.GetProperty("email").GetString());
        var workspace = Assert.Single(me.GetProperty("workspaces").EnumerateArray());
        Assert.Equal(System.Text.Json.JsonValueKind.Null, workspace.GetProperty("organizationId").ValueKind);

        ApiAssert.Status(await client.PostAsync("/auth/logout"), HttpStatusCode.NoContent);
        await ApiAssert.ProblemAsync(await client.GetAsync("/me"), HttpStatusCode.Unauthorized, "ERR-GEN-UNAUTHENTICATED");
    }

    [Fact]
    public async Task Name_is_saved_without_outer_spaces_and_no_username_is_exposed()
    {
        var email = Scenario.NewEmail();
        using var client = factory.CreateApiClient();

        ApiAssert.Status(await client.PostAsync("/auth/register", new { displayName = "  Trần Thị B  ", email, password = Scenario.Password }), HttpStatusCode.NoContent);
        var (userId, token) = factory.Emails.LastLink(email);
        await client.PostAsync("/auth/verify-email", new { userId, token });
        await client.PostAsync("/auth/login", new { email, password = Scenario.Password });

        var me = await ApiAssert.JsonAsync(await client.GetAsync("/me"));
        Assert.Equal("Trần Thị B", me.GetProperty("displayName").GetString());
        Assert.False(me.TryGetProperty("userName", out _));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Empty_name_is_refused_and_no_account_is_created(string displayName)
    {
        var email = Scenario.NewEmail();
        using var client = factory.CreateApiClient();

        await ApiAssert.ProblemAsync(
            await client.PostAsync("/auth/register", new { displayName, email, password = Scenario.Password }),
            HttpStatusCode.BadRequest,
            "ERR-GEN-VALIDATION");
        Assert.Equal(0, factory.Emails.CountFor(email));

        // The email is still free.
        ApiAssert.Status(await client.PostAsync("/auth/register", new { displayName = Scenario.DisplayName, email, password = Scenario.Password }), HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Name_can_have_100_characters_but_not_101()
    {
        using var client = factory.CreateApiClient();

        ApiAssert.Status(
            await client.PostAsync("/auth/register", new { displayName = new string('a', 100), email = Scenario.NewEmail(), password = Scenario.Password }),
            HttpStatusCode.NoContent);
        await ApiAssert.ProblemAsync(
            await client.PostAsync("/auth/register", new { displayName = new string('a', 101), email = Scenario.NewEmail(), password = Scenario.Password }),
            HttpStatusCode.BadRequest,
            "ERR-GEN-VALIDATION");
    }

    [Fact]
    public async Task Registering_without_a_name_is_a_validation_error()
    {
        using var client = factory.CreateApiClient();

        await ApiAssert.ProblemAsync(
            await client.PostAsync("/auth/register", new { email = Scenario.NewEmail(), password = Scenario.Password }),
            HttpStatusCode.BadRequest,
            "ERR-GEN-VALIDATION");
    }

    [Fact]
    public async Task Session_cookie_is_http_only_and_same_site_lax()
    {
        var (client, _, email) = await Scenario.SignedInUserAsync(factory);
        using (client)
        {
            var response = await client.PostAsync("/auth/login", new { email, password = Scenario.Password });

            var cookie = Assert.Single(response.Headers.GetValues("Set-Cookie"), c => c.StartsWith("cg_session=", StringComparison.Ordinal));
            Assert.Contains("httponly", cookie, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("samesite=lax", cookie, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public async Task Email_is_unique_ignoring_case()
    {
        var email = Scenario.NewEmail();
        using var client = factory.CreateApiClient();
        await client.PostAsync("/auth/register", new { displayName = Scenario.DisplayName, email, password = Scenario.Password });

        await ApiAssert.ProblemAsync(
            await client.PostAsync("/auth/register", new { displayName = Scenario.DisplayName, email = email.ToUpperInvariant(), password = Scenario.Password }),
            HttpStatusCode.Conflict,
            "ERR-AUTH-001");
    }

    [Theory]
    [InlineData("abc1234")] // too short
    [InlineData("abcdefgh")] // no digit
    [InlineData("12345678")] // no letter
    public async Task Weak_password_is_refused(string password)
    {
        var email = Scenario.NewEmail();
        using var client = factory.CreateApiClient();

        await ApiAssert.ProblemAsync(
            await client.PostAsync("/auth/register", new { displayName = Scenario.DisplayName, email, password }),
            HttpStatusCode.BadRequest,
            "ERR-AUTH-002");
        Assert.Equal(0, factory.Emails.CountFor(email));
    }

    [Fact]
    public async Task Upper_case_letters_count_as_letters()
    {
        using var client = factory.CreateApiClient();

        ApiAssert.Status(
            await client.PostAsync("/auth/register", new { displayName = Scenario.DisplayName, email = Scenario.NewEmail(), password = "ABCD1234" }),
            HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Invalid_verification_link_is_refused()
    {
        var email = Scenario.NewEmail();
        using var client = factory.CreateApiClient();
        await client.PostAsync("/auth/register", new { displayName = Scenario.DisplayName, email, password = Scenario.Password });
        var (userId, _) = factory.Emails.LastLink(email);

        await ApiAssert.ProblemAsync(
            await client.PostAsync("/auth/verify-email", new { userId, token = "not-a-token" }),
            HttpStatusCode.BadRequest,
            "ERR-AUTH-004");
    }

    [Fact]
    public async Task Links_last_ten_minutes()
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var options = scope.ServiceProvider.GetRequiredService<IOptions<DataProtectionTokenProviderOptions>>().Value;

        Assert.Equal(TimeSpan.FromMinutes(10), options.TokenLifespan);
    }

    [Fact]
    public async Task Failed_email_keeps_the_account_and_can_be_sent_again()
    {
        var email = Scenario.NewEmail();
        using var client = factory.CreateApiClient();
        factory.Emails.FailFor(email);

        await ApiAssert.ProblemAsync(
            await client.PostAsync("/auth/register", new { displayName = Scenario.DisplayName, email, password = Scenario.Password }),
            HttpStatusCode.ServiceUnavailable,
            "ERR-INTG-001");

        factory.Emails.StopFailingFor(email);
        ApiAssert.Status(await client.PostAsync("/auth/resend-verification", new { email }), HttpStatusCode.NoContent);
        Assert.Equal(1, factory.Emails.CountFor(email));
    }

    [Fact]
    public async Task Wrong_password_is_refused_and_five_in_a_row_lock_the_email()
    {
        var (client, _, email) = await Scenario.SignedInUserAsync(factory);
        using (client)
        {
            for (var attempt = 1; attempt <= 4; attempt++)
            {
                await ApiAssert.ProblemAsync(
                    await client.PostAsync("/auth/login", new { email, password = "Sai-mat-khau-1" }),
                    HttpStatusCode.Unauthorized,
                    "ERR-AUTH-003");
            }

            await ApiAssert.ProblemAsync(
                await client.PostAsync("/auth/login", new { email, password = "Sai-mat-khau-1" }),
                HttpStatusCode.Forbidden,
                "ERR-AUTH-005");

            // SEC-AUTH-006: even the right password is refused during the lockout...
            await ApiAssert.ProblemAsync(
                await client.PostAsync("/auth/login", new { email, password = Scenario.Password }),
                HttpStatusCode.Forbidden,
                "ERR-AUTH-005");

            // ...but resetting the password still works and ends it (ERR-AUTH-005).
            ApiAssert.Status(await client.PostAsync("/auth/forgot-password", new { email }), HttpStatusCode.NoContent);
            var (userId, token) = factory.Emails.LastLink(email);
            ApiAssert.Status(await client.PostAsync("/auth/reset-password", new { userId, token, newPassword = "Mat-khau-2" }), HttpStatusCode.NoContent);
            ApiAssert.Status(await client.PostAsync("/auth/login", new { email, password = "Mat-khau-2" }), HttpStatusCode.NoContent);
        }
    }

    [Fact]
    public async Task Unknown_email_gets_the_same_answer_as_a_known_one_but_no_email()
    {
        var unknown = Scenario.NewEmail();
        using var client = factory.CreateApiClient();

        ApiAssert.Status(await client.PostAsync("/auth/forgot-password", new { email = unknown }), HttpStatusCode.NoContent);

        Assert.Equal(0, factory.Emails.CountFor(unknown));
    }

    [Fact]
    public async Task Reset_changes_the_password_and_the_link_works_only_once()
    {
        var (client, _, email) = await Scenario.SignedInUserAsync(factory);
        using (client)
        {
            await client.PostAsync("/auth/forgot-password", new { email });
            var (userId, token) = factory.Emails.LastLink(email);

            await ApiAssert.ProblemAsync(
                await client.PostAsync("/auth/reset-password", new { userId, token, newPassword = "weak" }),
                HttpStatusCode.BadRequest,
                "ERR-AUTH-002");

            ApiAssert.Status(await client.PostAsync("/auth/reset-password", new { userId, token, newPassword = "Mat-khau-2" }), HttpStatusCode.NoContent);

            await ApiAssert.ProblemAsync(
                await client.PostAsync("/auth/reset-password", new { userId, token, newPassword = "Mat-khau-3" }),
                HttpStatusCode.BadRequest,
                "ERR-AUTH-004");
            await ApiAssert.ProblemAsync(
                await client.PostAsync("/auth/login", new { email, password = Scenario.Password }),
                HttpStatusCode.Unauthorized,
                "ERR-AUTH-003");
        }
    }

    [Fact]
    public async Task Write_without_csrf_token_is_refused()
    {
        using var client = factory.CreateApiClient();

        var response = await client.Http.PostAsync(
            "/api/v1/auth/forgot-password",
            System.Net.Http.Json.JsonContent.Create(new { email = Scenario.NewEmail() }),
            TestContext.Current.CancellationToken);

        await ApiAssert.ProblemAsync(response, HttpStatusCode.BadRequest, "ERR-GEN-CSRF");
    }

    [Fact]
    public async Task Csrf_token_from_before_login_stops_working_after_login()
    {
        var email = Scenario.NewEmail();
        using var client = factory.CreateApiClient();
        await client.PostAsync("/auth/register", new { displayName = Scenario.DisplayName, email, password = Scenario.Password });
        var (userId, token) = factory.Emails.LastLink(email);
        await client.PostAsync("/auth/verify-email", new { userId, token });
        var anonymousToken = await client.GetCsrfTokenAsync();
        await client.PostAsync("/auth/login", new { email, password = Scenario.Password });

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/logout");
        request.Headers.Add("X-CSRF-TOKEN", anonymousToken);
        var response = await client.Http.SendAsync(request, TestContext.Current.CancellationToken);

        await ApiAssert.ProblemAsync(response, HttpStatusCode.BadRequest, "ERR-GEN-CSRF");
    }

    [Fact]
    public async Task Production_api_behind_a_load_balancer_that_ends_tls_answers()
    {
        // In Production the antiforgery cookie is Secure-only, and the API itself only sees http.
        using var http = factory.WithWebHostBuilder(builder => builder.UseEnvironment("Production")).CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/auth/csrf");
        request.Headers.Add("X-Forwarded-Proto", "https");

        ApiAssert.Status(await http.SendAsync(request, TestContext.Current.CancellationToken), HttpStatusCode.OK);
    }

    [Fact]
    public async Task Missing_field_is_a_validation_error()
    {
        using var client = factory.CreateApiClient();

        await ApiAssert.ProblemAsync(
            await client.PostAsync("/auth/register", new { displayName = Scenario.DisplayName, email = Scenario.NewEmail() }),
            HttpStatusCode.BadRequest,
            "ERR-GEN-VALIDATION");
    }

    [Theory]
    [InlineData("/me")]
    [InlineData("/contracts")]
    [InlineData("/jobs/019a0000-0000-7000-8000-000000000000")]
    public async Task Everything_else_needs_a_session(string path)
    {
        // SEC-AUTH-001
        using var client = factory.CreateApiClient();

        await ApiAssert.ProblemAsync(await client.GetAsync(path), HttpStatusCode.Unauthorized, "ERR-GEN-UNAUTHENTICATED");
    }

    [Fact]
    public async Task Password_is_stored_hashed()
    {
        // SEC-AUTH-002
        var (client, userId, _) = await Scenario.SignedInUserAsync(factory);
        client.Dispose();

        await using var scope = factory.Services.CreateAsyncScope();
        var user = await scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>().FindByIdAsync(userId.ToString());

        Assert.DoesNotContain(Scenario.Password, user!.PasswordHash, StringComparison.Ordinal);
    }
}
