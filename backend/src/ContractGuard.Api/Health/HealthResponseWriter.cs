using System.Text.Json;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace ContractGuard.Api.Health;

// Writes {"status":"Healthy","checks":{"postgres":"Healthy","s3":"Unhealthy: ..."}}
// instead of the default plain "Healthy", so a failing dependency is visible at a glance.
internal static class HealthResponseWriter
{
    public static Task WriteAsync(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = "application/json; charset=utf-8";
        var body = new
        {
            status = report.Status.ToString(),
            checks = report.Entries.ToDictionary(
                entry => entry.Key,
                entry => entry.Value.Description is null
                    ? entry.Value.Status.ToString()
                    : $"{entry.Value.Status}: {entry.Value.Description}")
        };
        return context.Response.WriteAsync(JsonSerializer.Serialize(body));
    }
}
