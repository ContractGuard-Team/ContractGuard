using System.Net;
using System.Text.Json;
using ContractGuard.Application.Analysis;
using ContractGuard.Domain.Analysis;
using ContractGuard.Domain.Contracts;
using ContractGuard.Infrastructure.Persistence;
using Json.Schema;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ContractGuard.IntegrationTests;

// F-CTR-001 and F-ANA-001: from the upload to the clauses in the API.
[Collection(ApiCollection.Name)]
public sealed class UploadAndAnalysisTests(ApiFactory factory)
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private static readonly byte[] PngBytes = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00];
    private static readonly byte[] ExeBytes = "MZ not a document"u8.ToArray();

    [Theory]
    [InlineData("virus.exe", 100, "ERR-UPL-001")]
    [InlineData("no-extension", 100, "ERR-UPL-001")]
    [InlineData("big.pdf", (20 * 1024 * 1024) + 1, "ERR-UPL-003")]
    [InlineData(".pdf", 100, "ERR-GEN-VALIDATION")]
    [InlineData("empty.pdf", 0, "ERR-GEN-VALIDATION")]
    public async Task Upload_ticket_is_refused_for_a_bad_file(string fileName, long sizeBytes, string code)
    {
        var (client, _, _) = await Scenario.SignedInUserAsync(factory);
        using (client)
        {
            await ApiAssert.ProblemAsync(await client.PostAsync("/uploads", new { fileName, sizeBytes }), HttpStatusCode.BadRequest, code);
        }
    }

    [Fact]
    public async Task Exactly_20_MB_is_accepted()
    {
        // EDGE-UPL-002
        var (client, _, _) = await Scenario.SignedInUserAsync(factory);
        using (client)
        {
            ApiAssert.Status(
                await client.PostAsync("/uploads", new { fileName = "big.pdf", sizeBytes = 20L * 1024 * 1024 }),
                HttpStatusCode.OK);
        }
    }

    [Fact]
    public async Task Upload_creates_a_new_contract_with_v1_owned_by_the_uploader()
    {
        var (client, userId, _) = await Scenario.SignedInUserAsync(factory);
        using (client)
        {
            var (contractId, _) = await Scenario.UploadContractAsync(factory, client);

            var detail = await ApiAssert.JsonAsync(await client.GetAsync($"/contracts/{contractId}"));
            Assert.Equal("hop-dong", detail.GetProperty("contractName").GetString());
            Assert.Equal("new", detail.GetProperty("approvalStatus").GetString());
            var version = detail.GetProperty("latestVersion");
            Assert.Equal(1, version.GetProperty("versionNumber").GetInt32());
            Assert.Equal("pdf", version.GetProperty("fileType").GetString());
            Assert.Equal(Scenario.PdfBytes.Length, version.GetProperty("fileSizeBytes").GetInt64());
            Assert.Equal(JsonValueKind.Null, version.GetProperty("context").ValueKind);
            Assert.Equal(JsonValueKind.Null, detail.GetProperty("latestJob").ValueKind);

            await using var scope = factory.CreateSystemScope();
            var saved = await scope.ServiceProvider.GetRequiredService<AppDbContext>().Contracts
                .Include(c => c.Versions)
                .SingleAsync(c => c.ContractId == contractId, Ct);
            Assert.Equal(userId, saved.OwnerUserAccountId);
            Assert.Null(saved.OrganizationId);
            Assert.StartsWith($"quarantine/{userId}/", saved.LatestVersion.SourceFileKey, StringComparison.Ordinal);
        }
    }

    [Fact]
    public async Task One_upload_makes_one_contract()
    {
        var (client, _, _) = await Scenario.SignedInUserAsync(factory);
        using (client)
        {
            var uploadId = await Scenario.UploadFileAsync(factory, client, "a.pdf", Scenario.PdfBytes);
            var first = await ApiAssert.JsonAsync(await client.PostAsync("/contracts", new { uploadId, fileName = "a.pdf" }));

            await ApiAssert.ProblemAsync(
                await client.PostAsync("/contracts", new { uploadId, fileName = "a.pdf" }),
                HttpStatusCode.BadRequest,
                "ERR-GEN-VALIDATION");

            Assert.Equal(1, (await ApiAssert.JsonAsync(await client.GetAsync("/contracts"))).GetArrayLength());

            // The refusal leaves the file of the first contract alone.
            var contractId = first.GetProperty("contractId").GetGuid();
            var versionId = first.GetProperty("contractVersionId").GetGuid();
            ApiAssert.Status(await client.GetAsync($"/contracts/{contractId}/versions/{versionId}/file"), HttpStatusCode.OK);
        }
    }

    [Fact]
    public async Task Upload_that_never_arrived_is_not_found()
    {
        var (client, _, _) = await Scenario.SignedInUserAsync(factory);
        using (client)
        {
            await ApiAssert.ProblemAsync(
                await client.PostAsync("/contracts", new { uploadId = Guid.NewGuid(), fileName = "a.pdf" }),
                HttpStatusCode.NotFound,
                "ERR-GEN-NOT-FOUND");
        }
    }

    [Fact]
    public async Task Upload_of_another_user_cannot_be_claimed()
    {
        var (alice, _, _) = await Scenario.SignedInUserAsync(factory);
        var (bob, _, _) = await Scenario.SignedInUserAsync(factory);
        using (alice)
        using (bob)
        {
            var aliceUpload = await Scenario.UploadFileAsync(factory, alice, "a.pdf", Scenario.PdfBytes);

            await ApiAssert.ProblemAsync(
                await bob.PostAsync("/contracts", new { uploadId = aliceUpload, fileName = "a.pdf" }),
                HttpStatusCode.NotFound,
                "ERR-GEN-NOT-FOUND");
        }
    }

    [Fact]
    public async Task Content_that_is_no_allowed_type_is_refused_and_deleted()
    {
        await AssertUploadRefusedAsync("a.pdf", ExeBytes, "ERR-UPL-001");
    }

    [Fact]
    public async Task Content_that_differs_from_the_extension_is_refused_and_deleted()
    {
        await AssertUploadRefusedAsync("a.pdf", PngBytes, "ERR-UPL-002");
    }

    [Fact]
    public async Task File_that_arrived_too_big_is_refused_and_deleted()
    {
        var bytes = new byte[(20 * 1024 * 1024) + 1];
        Scenario.PdfBytes.CopyTo(bytes, 0);

        // The ticket is asked for with a small size; storage holds what really arrived.
        var (client, _, _) = await Scenario.SignedInUserAsync(factory);
        using (client)
        {
            var ticket = await ApiAssert.JsonAsync(await client.PostAsync("/uploads", new { fileName = "a.pdf", sizeBytes = 10 }));
            var key = ticket.GetProperty("fields").GetProperty("key").GetString()!;
            factory.Storage.PutBytes(key, bytes);

            await ApiAssert.ProblemAsync(
                await client.PostAsync("/contracts", new { uploadId = ticket.GetProperty("uploadId").GetGuid(), fileName = "a.pdf" }),
                HttpStatusCode.BadRequest,
                "ERR-UPL-003");
            Assert.False(factory.Storage.Exists(key));
        }
    }

    [Fact]
    public async Task Missing_required_context_field_names_the_field_and_starts_nothing()
    {
        var (client, _, _) = await Scenario.SignedInUserAsync(factory);
        using (client)
        {
            var (contractId, versionId) = await Scenario.UploadContractAsync(factory, client);

            var response = await client.PutAsync(
                $"/contracts/{contractId}/versions/{versionId}/context",
                new { contractType = "msa", hasPersonalData = false });

            var body = await response.Content.ReadAsStringAsync(Ct);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Contains("ERR-CTR-002", body, StringComparison.Ordinal);
            Assert.Contains("OwnParty", body, StringComparison.Ordinal);
            var detail = await ApiAssert.JsonAsync(await client.GetAsync($"/contracts/{contractId}"));
            Assert.Equal("new", detail.GetProperty("approvalStatus").GetString());
        }
    }

    [Fact]
    public async Task Unknown_enum_value_is_a_validation_error()
    {
        var (client, _, _) = await Scenario.SignedInUserAsync(factory);
        using (client)
        {
            var (contractId, versionId) = await Scenario.UploadContractAsync(factory, client);

            await ApiAssert.ProblemAsync(
                await client.PutAsync($"/contracts/{contractId}/versions/{versionId}/context", new { ownParty = "nobody", contractType = "msa", hasPersonalData = true }),
                HttpStatusCode.BadRequest,
                "ERR-GEN-VALIDATION");
        }
    }

    [Fact]
    public async Task Confirming_the_context_starts_the_analysis()
    {
        var (client, _, _) = await Scenario.SignedInUserAsync(factory);
        using (client)
        {
            var (contractId, versionId) = await Scenario.UploadContractAsync(factory, client);

            var jobId = await Scenario.StartAnalysisAsync(client, contractId, versionId);

            var message = factory.Queue.Find(jobId);
            Assert.NotNull(message);
            Assert.Equal(AnalysisEventTypes.AnalysisRequested, message.EventType);
            Assert.Equal($"results/{jobId}/request.json", message.RequestKey);

            var job = await ApiAssert.JsonAsync(await client.GetAsync($"/jobs/{jobId}"));
            Assert.Equal("running", job.GetProperty("status").GetString());
            Assert.Equal("reading_file", job.GetProperty("step").GetString());

            var detail = await ApiAssert.JsonAsync(await client.GetAsync($"/contracts/{contractId}"));
            Assert.Equal("analyzing", detail.GetProperty("approvalStatus").GetString());
            Assert.Equal("buyer", detail.GetProperty("latestVersion").GetProperty("context").GetProperty("ownParty").GetString());
            Assert.True(detail.GetProperty("analysisAvailable").GetBoolean());
        }
    }

    [Fact]
    public async Task Without_a_worker_the_context_is_saved_and_nothing_is_started()
    {
        var (client, _, _) = await Scenario.SignedInUserAsync(factory, factory.CreateApiClientWithSetting("Analysis:WorkerEnabled", "false"));
        using (client)
        {
            var (contractId, versionId) = await Scenario.UploadContractAsync(factory, client);

            ApiAssert.Status(
                await client.PutAsync($"/contracts/{contractId}/versions/{versionId}/context", Scenario.Context),
                HttpStatusCode.NoContent);
            ApiAssert.Status(
                await client.PutAsync(
                    $"/contracts/{contractId}/versions/{versionId}/context",
                    new { ownParty = "seller", contractType = "msa", hasPersonalData = false }),
                HttpStatusCode.NoContent);

            var detail = await ApiAssert.JsonAsync(await client.GetAsync($"/contracts/{contractId}"));
            Assert.False(detail.GetProperty("analysisAvailable").GetBoolean());
            Assert.Equal("new", detail.GetProperty("approvalStatus").GetString());
            Assert.Equal("seller", detail.GetProperty("latestVersion").GetProperty("context").GetProperty("ownParty").GetString());
            Assert.Equal(JsonValueKind.Null, detail.GetProperty("latestJob").ValueKind);

            await using var scope = factory.CreateSystemScope();
            Assert.False(await scope.ServiceProvider.GetRequiredService<AppDbContext>().AnalysisRuns.AnyAsync(r => r.ContractId == contractId, Ct));
        }
    }

    [Fact]
    public async Task Link_to_the_file_carries_its_name_and_checked_type()
    {
        var (client, userId, _) = await Scenario.SignedInUserAsync(factory);
        using (client)
        {
            var (contractId, versionId) = await Scenario.UploadContractAsync(factory, client);

            var link = await ApiAssert.JsonAsync(await client.GetAsync($"/contracts/{contractId}/versions/{versionId}/file"));

            var url = link.GetProperty("url").GetString()!;
            Assert.StartsWith($"http://storage.test/quarantine/{userId}/", url, StringComparison.Ordinal);
            Assert.Contains("name=hop-dong.pdf", url, StringComparison.Ordinal);
            Assert.Contains("type=application%2Fpdf", url, StringComparison.Ordinal);
        }
    }

    [Fact]
    public async Task Link_to_a_file_that_left_storage_is_not_found_and_the_contract_stays()
    {
        var (client, _, _) = await Scenario.SignedInUserAsync(factory);
        using (client)
        {
            var (contractId, versionId) = await Scenario.UploadContractAsync(factory, client);
            await using (var scope = factory.CreateSystemScope())
            {
                var version = await scope.ServiceProvider.GetRequiredService<AppDbContext>().ContractVersions.SingleAsync(v => v.ContractVersionId == versionId, Ct);
                await factory.Storage.DeleteAsync(version.SourceFileKey, Ct);
            }

            await ApiAssert.ProblemAsync(
                await client.GetAsync($"/contracts/{contractId}/versions/{versionId}/file"),
                HttpStatusCode.NotFound,
                "ERR-GEN-NOT-FOUND");
            ApiAssert.Status(await client.GetAsync($"/contracts/{contractId}"), HttpStatusCode.OK);
        }
    }

    [Fact]
    public async Task Link_to_a_version_of_another_contract_is_not_found()
    {
        var (client, _, _) = await Scenario.SignedInUserAsync(factory);
        using (client)
        {
            var (contractId, _) = await Scenario.UploadContractAsync(factory, client);
            var (_, otherVersionId) = await Scenario.UploadContractAsync(factory, client);

            await ApiAssert.ProblemAsync(
                await client.GetAsync($"/contracts/{contractId}/versions/{otherVersionId}/file"),
                HttpStatusCode.NotFound,
                "ERR-GEN-NOT-FOUND");
        }
    }

    [Fact]
    public async Task Request_file_follows_the_schema_shared_with_the_worker()
    {
        var (client, userId, _) = await Scenario.SignedInUserAsync(factory);
        using (client)
        {
            var (contractId, versionId) = await Scenario.UploadContractAsync(factory, client);
            var jobId = await Scenario.StartAnalysisAsync(client, contractId, versionId);

            var json = factory.Storage.GetText($"results/{jobId}/request.json");

            var schema = JsonSchema.FromText(await File.ReadAllTextAsync(Seed.RepoFile("contracts/analysis-request.v1.schema.json"), Ct));
            using var document = JsonDocument.Parse(json);
            var result = schema.Evaluate(document.RootElement, new EvaluationOptions { RequireFormatValidation = true, OutputFormat = OutputFormat.List });
            Assert.True(result.IsValid, json);

            var root = document.RootElement;
            Assert.Equal($"personal:{userId}", root.GetProperty("tenant_id").GetString());
            Assert.Equal("1", root.GetProperty("contract_version").GetString());
            Assert.Equal("service_agreement", root.GetProperty("context").GetProperty("contract_type").GetString());
            Assert.Equal($"results/{jobId}/extraction.json", root.GetProperty("result_key").GetString());
            Assert.Equal($"clean/personal-{userId}/{contractId}/v1/source.pdf", root.GetProperty("clean_file_key").GetString());
        }
    }

    [Fact]
    public async Task Second_start_during_a_running_analysis_is_refused()
    {
        // EDGE-ANA-003
        var (client, _, _) = await Scenario.SignedInUserAsync(factory);
        using (client)
        {
            var (contractId, versionId) = await Scenario.UploadContractAsync(factory, client);
            await Scenario.StartAnalysisAsync(client, contractId, versionId);

            await ApiAssert.ProblemAsync(
                await client.PutAsync($"/contracts/{contractId}/versions/{versionId}/context", Scenario.Context),
                HttpStatusCode.Conflict,
                "ERR-CTR-003");
        }
    }

    [Fact]
    public async Task Database_refuses_a_second_running_analysis_of_a_contract()
    {
        // The race the status check cannot catch: two requests that both saw "not analyzing".
        var (client, _, _) = await Scenario.SignedInUserAsync(factory);
        using (client)
        {
            var (contractId, versionId) = await Scenario.UploadContractAsync(factory, client);
            await Scenario.StartAnalysisAsync(client, contractId, versionId);

            await using var scope = factory.CreateSystemScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.AnalysisRuns.Add(AnalysisRun.Start(contractId, versionId, DateTimeOffset.UtcNow));

            await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync(Ct));
        }
    }

    [Fact]
    public async Task Clauses_are_available_after_the_worker_completes()
    {
        var (client, _, _) = await Scenario.SignedInUserAsync(factory);
        using (client)
        {
            var (contractId, versionId) = await Scenario.UploadContractAsync(factory, client);
            var jobId = await Scenario.StartAnalysisAsync(client, contractId, versionId);

            await Scenario.CompleteJobAsync(factory, jobId, contractId);

            var detail = await ApiAssert.JsonAsync(await client.GetAsync($"/contracts/{contractId}"));
            Assert.Equal("pending_review", detail.GetProperty("approvalStatus").GetString());
            Assert.Equal(jobId, detail.GetProperty("completedJobId").GetGuid());

            var clauses = await ApiAssert.JsonAsync(await client.GetAsync($"/jobs/{jobId}/clauses"));
            Assert.Equal(8, clauses.GetArrayLength());
            var orders = clauses.EnumerateArray().Select(c => c.GetProperty("clauseOrder").GetInt32()).ToList();
            Assert.Equal(orders.Order(), orders);
            Assert.Matches("^C[0-9]{2}$", clauses[0].GetProperty("clauseType").GetString()!);
            Assert.True(clauses[0].GetProperty("location").GetProperty("pageStart").GetInt32() >= 1);
        }
    }

    [Fact]
    public async Task Failed_rerun_keeps_the_previous_result_and_can_be_run_again()
    {
        // FR-ANA-014, FR-ANA-015: only a successful run replaces the old one.
        var (client, _, _) = await Scenario.SignedInUserAsync(factory);
        using (client)
        {
            var (contractId, versionId) = await Scenario.UploadContractAsync(factory, client);
            var firstJob = await Scenario.StartAnalysisAsync(client, contractId, versionId);
            await Scenario.CompleteJobAsync(factory, firstJob, contractId);

            var secondJob = await Scenario.StartAnalysisAsync(client, contractId, versionId);
            await using (var scope = factory.CreateSystemScope())
            {
                await scope.ServiceProvider.GetRequiredService<AnalysisResultHandler>().HandleAsync(
                    new AnalysisResultMessage(AnalysisEventTypes.Failed, secondJob, null, null, "Model timed out.", null),
                    Ct);
            }

            var detail = await ApiAssert.JsonAsync(await client.GetAsync($"/contracts/{contractId}"));
            Assert.Equal("pending_review", detail.GetProperty("approvalStatus").GetString());
            Assert.Equal(secondJob, detail.GetProperty("latestJob").GetProperty("jobId").GetGuid());
            Assert.Equal("Model timed out.", detail.GetProperty("latestJob").GetProperty("failureReason").GetString());
            Assert.Equal(firstJob, detail.GetProperty("completedJobId").GetGuid());

            var thirdJob = await Scenario.StartAnalysisAsync(client, contractId, versionId);
            Assert.NotEqual(secondJob, thirdJob);
        }
    }

    private async Task AssertUploadRefusedAsync(string fileName, byte[] bytes, string code)
    {
        var (client, _, _) = await Scenario.SignedInUserAsync(factory);
        using (client)
        {
            var ticket = await ApiAssert.JsonAsync(await client.PostAsync("/uploads", new { fileName, sizeBytes = bytes.Length }));
            var key = ticket.GetProperty("fields").GetProperty("key").GetString()!;
            factory.Storage.PutBytes(key, bytes);

            await ApiAssert.ProblemAsync(
                await client.PostAsync("/contracts", new { uploadId = ticket.GetProperty("uploadId").GetGuid(), fileName }),
                HttpStatusCode.BadRequest,
                code);
            Assert.False(factory.Storage.Exists(key));

            var library = await ApiAssert.JsonAsync(await client.GetAsync("/contracts"));
            Assert.Equal(0, library.GetArrayLength());
        }
    }
}
