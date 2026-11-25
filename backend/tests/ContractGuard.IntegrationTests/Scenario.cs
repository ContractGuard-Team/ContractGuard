using System.Net;
using ContractGuard.Application.Analysis;
using Microsoft.Extensions.DependencyInjection;

namespace ContractGuard.IntegrationTests;

// The steps most tests start from, done through the real API.
internal static class Scenario
{
    public const string Password = "Mat-khau-1";
    public const string DisplayName = "Nguyễn Văn An";

    public static byte[] PdfBytes { get; } = "%PDF-1.7 synthetic test file"u8.ToArray();

    public static readonly object Context = new { ownParty = "buyer", contractType = "service_agreement", hasPersonalData = true };

    public static string NewEmail() => $"user-{Guid.NewGuid():N}@example.com";

    // Register, open the verification link, log in. Returns the client holding the session.
    public static async Task<(ApiClient Client, Guid UserId, string Email)> SignedInUserAsync(ApiFactory factory, ApiClient? existingClient = null)
    {
        var email = NewEmail();
        var client = existingClient ?? factory.CreateApiClient();
        ApiAssert.Status(await client.PostAsync("/auth/register", new { displayName = DisplayName, email, password = Password }), HttpStatusCode.NoContent);

        var (userId, token) = factory.Emails.LastLink(email);
        ApiAssert.Status(await client.PostAsync("/auth/verify-email", new { userId, token }), HttpStatusCode.NoContent);
        ApiAssert.Status(await client.PostAsync("/auth/login", new { email, password = Password }), HttpStatusCode.NoContent);
        return (client, userId, email);
    }

    // Ask for an upload ticket, "upload" the bytes, create the contract.
    public static async Task<(Guid ContractId, Guid VersionId)> UploadContractAsync(ApiFactory factory, ApiClient client, Guid? workspace = null)
    {
        var uploadId = await UploadFileAsync(factory, client, "hop-dong.pdf", PdfBytes, workspace);
        var response = await client.PostAsync("/contracts", new { uploadId, fileName = "hop-dong.pdf" }, workspace);
        ApiAssert.Status(response, HttpStatusCode.OK);
        var body = await ApiAssert.JsonAsync(response);
        return (body.GetProperty("contractId").GetGuid(), body.GetProperty("contractVersionId").GetGuid());
    }

    public static async Task<Guid> UploadFileAsync(ApiFactory factory, ApiClient client, string fileName, byte[] bytes, Guid? workspace = null)
    {
        var response = await client.PostAsync("/uploads", new { fileName, sizeBytes = bytes.Length }, workspace);
        ApiAssert.Status(response, HttpStatusCode.OK);
        var ticket = await ApiAssert.JsonAsync(response);
        factory.Storage.PutBytes(ticket.GetProperty("fields").GetProperty("key").GetString()!, bytes);
        return ticket.GetProperty("uploadId").GetGuid();
    }

    public static async Task SaveContextAsync(ApiClient client, Guid contractId, Guid versionId, Guid? workspace = null) =>
        ApiAssert.Status(await client.PutAsync($"/contracts/{contractId}/versions/{versionId}/context", Context, workspace), HttpStatusCode.NoContent);

    // Save the context, then press "start analysis" (CR-ANA-002: saving alone does not start it).
    public static async Task<Guid> StartAnalysisAsync(ApiClient client, Guid contractId, Guid versionId, Guid? workspace = null)
    {
        await SaveContextAsync(client, contractId, versionId, workspace);
        var response = await client.PostAsync($"/contracts/{contractId}/versions/{versionId}/analyses", workspace: workspace);
        ApiAssert.Status(response, HttpStatusCode.Accepted);
        return (await ApiAssert.JsonAsync(response)).GetProperty("jobId").GetGuid();
    }

    // Upload, confirm context, let the "worker" complete with the example result.
    public static async Task<(Guid ContractId, Guid VersionId, Guid JobId)> AnalyzedContractAsync(ApiFactory factory, ApiClient client, Guid? workspace = null)
    {
        var (contractId, versionId) = await UploadContractAsync(factory, client, workspace);
        var jobId = await StartAnalysisAsync(client, contractId, versionId, workspace);
        await CompleteJobAsync(factory, jobId, contractId);
        return (contractId, versionId, jobId);
    }

    // Plays the worker: the example result file plus a "completed" message through the real handler.
    public static async Task CompleteJobAsync(ApiFactory factory, Guid jobId, Guid contractId)
    {
        var key = $"results/{jobId}/extraction.json";
        factory.Storage.PutText(key, Seed.ExampleExtractionJson(jobId, contractId));
        await using var scope = factory.CreateSystemScope();
        var outcome = await scope.ServiceProvider.GetRequiredService<AnalysisResultHandler>()
            .HandleAsync(new AnalysisResultMessage(AnalysisEventTypes.Completed, jobId, null, key, null, null), TestContext.Current.CancellationToken);
        Assert.Equal(ResultOutcome.Applied, outcome);
    }
}
