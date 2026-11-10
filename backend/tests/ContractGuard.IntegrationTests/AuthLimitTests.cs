using System.Net;

namespace ContractGuard.IntegrationTests;

// SEC-AUTH-007, FR-AUTH-014: limits that stop password guessing and email flooding.
[Collection(ApiCollection.Name)]
public sealed class AuthLimitTests(ApiFactory factory)
{
    [Fact]
    public async Task Login_attempts_per_address_are_limited()
    {
        using var client = factory.CreateApiClientWithSetting("RateLimits:LoginPerMinute", "2");

        for (var i = 0; i < 2; i++)
        {
            await ApiAssert.ProblemAsync(await client.PostAsync("/auth/login", new { email = "nobody@example.com", password = "Sai-mat-khau-1" }), HttpStatusCode.Unauthorized, "ERR-AUTH-003");
        }

        var refused = await client.PostAsync("/auth/login", new { email = "nobody@example.com", password = "Sai-mat-khau-1" });
        await ApiAssert.ProblemAsync(refused, HttpStatusCode.TooManyRequests, "ERR-AUTH-006");
    }

    [Fact]
    public async Task Reset_email_is_not_sent_again_within_3_minutes()
    {
        var (client, _, email) = await Scenario.SignedInUserAsync(factory);
        using (client)
        {
            ApiAssert.Status(await client.PostAsync("/auth/forgot-password", new { email }), HttpStatusCode.NoContent);
            var after = factory.Emails.CountFor(email);

            // Same answer as a sent email, so nobody learns about the limit or the account.
            ApiAssert.Status(await client.PostAsync("/auth/forgot-password", new { email }), HttpStatusCode.NoContent);

            Assert.Equal(after, factory.Emails.CountFor(email));
        }
    }
}
