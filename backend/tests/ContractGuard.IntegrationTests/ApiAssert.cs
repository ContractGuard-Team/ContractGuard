using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace ContractGuard.IntegrationTests;

internal static class ApiAssert
{
    // The response is Problem Details with this status and ERR-* code (SD-09).
    public static async Task ProblemAsync(HttpResponseMessage response, HttpStatusCode status, string code)
    {
        var text = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        Assert.True(response.StatusCode == status, $"Expected {status}, got {response.StatusCode}: {text}");
        Assert.Equal(code, JsonDocument.Parse(text).RootElement.GetProperty("code").GetString());
    }

    public static void Status(HttpResponseMessage response, HttpStatusCode status)
    {
        if (response.StatusCode != status)
        {
            Assert.Fail($"Expected {status}, got {response.StatusCode}: {response.Content.ReadAsStringAsync().Result}");
        }
    }

    public static async Task<JsonElement> JsonAsync(HttpResponseMessage response) =>
        await response.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);
}
