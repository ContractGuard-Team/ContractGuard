using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace ContractGuard.IntegrationTests;

// Calls the API like the frontend does: one cookie jar per client, a fresh CSRF token for each write.
public sealed class ApiClient(HttpClient http) : IDisposable
{
    public HttpClient Http => http;

    public Task<HttpResponseMessage> GetAsync(string path, Guid? workspace = null) =>
        SendAsync(HttpMethod.Get, path, null, workspace);

    public Task<HttpResponseMessage> PostAsync(string path, object? body = null, Guid? workspace = null) =>
        SendAsync(HttpMethod.Post, path, body, workspace);

    public Task<HttpResponseMessage> PutAsync(string path, object? body = null, Guid? workspace = null) =>
        SendAsync(HttpMethod.Put, path, body, workspace);

    public async Task<T> GetJsonAsync<T>(string path, Guid? workspace = null)
    {
        var response = await GetAsync(path, workspace);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<T>(Json, Ct))!;
    }

    public async Task<string> GetCsrfTokenAsync()
    {
        var response = await http.GetAsync("/api/v1/auth/csrf", Ct);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(Ct);
        return body.GetProperty("token").GetString()!;
    }

    public void Dispose() => http.Dispose();

    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private async Task<HttpResponseMessage> SendAsync(HttpMethod method, string path, object? body, Guid? workspace)
    {
        using var request = new HttpRequestMessage(method, "/api/v1" + path);
        if (method != HttpMethod.Get)
        {
            request.Headers.Add("X-CSRF-TOKEN", await GetCsrfTokenAsync());
        }

        if (body is not null)
        {
            request.Content = JsonContent.Create(body);
        }

        if (workspace is not null)
        {
            request.Headers.Add("X-Workspace-Id", workspace.ToString());
        }

        return await http.SendAsync(request, Ct);
    }
}
