using System.Net;
using System.Text.Json;

namespace ContractGuard.IntegrationTests;

// Phase 2: what a completed run stores besides clauses, read and edited through the API.
[Collection(ApiCollection.Name)]
public sealed class ResultsTests(ApiFactory factory)
{
    [Fact]
    public async Task Completed_run_exposes_findings_checks_scores_and_metadata()
    {
        var (client, _, _) = await Scenario.SignedInUserAsync(factory);
        using (client)
        {
            var (contractId, versionId, jobId) = await Scenario.AnalyzedContractAsync(factory, client);

            var result = await ApiAssert.JsonAsync(await client.GetAsync($"/jobs/{jobId}/result"));
            var findings = result.GetProperty("findings");
            Assert.Equal(4, findings.GetArrayLength());

            // Most severe first, all fields of BR-RISK-014 present.
            var first = findings[0];
            Assert.Equal("critical", first.GetProperty("severity").GetString());
            Assert.Equal("cybersecurity", first.GetProperty("primaryDomain").GetString());
            Assert.Equal("cl-006", first.GetProperty("clauseKey").GetString());
            Assert.Equal("open", first.GetProperty("handlingStatus").GetString());
            Assert.Equal(1, first.GetProperty("evidence").GetArrayLength());
            Assert.Equal("NIST SP 800-161r1", first.GetProperty("references")[0].GetProperty("source").GetString());
            Assert.Equal("missing_clause", findings[3].GetProperty("kind").GetString());

            Assert.Equal(20, result.GetProperty("checks").GetArrayLength());
            Assert.Equal("CY-01", result.GetProperty("checks")[0].GetProperty("checkCode").GetString());

            var score = result.GetProperty("score");
            Assert.Equal(72, score.GetProperty("overallScore").GetInt32());
            Assert.Equal("high", score.GetProperty("riskLevel").GetString());
            var compliance = score.GetProperty("domains").EnumerateArray().Single(d => d.GetProperty("domain").GetString() == "compliance");
            Assert.False(compliance.GetProperty("applicable").GetBoolean());
            Assert.Equal(JsonValueKind.Null, compliance.GetProperty("score").ValueKind);

            Assert.Equal("example-rules-0", result.GetProperty("trace").GetProperty("ruleVersion").GetString());
            Assert.StartsWith("Chưa nên ký", result.GetProperty("overallRecommendation").GetString(), StringComparison.Ordinal);

            var version = (await ApiAssert.JsonAsync(await client.GetAsync($"/contracts/{contractId}"))).GetProperty("version");
            Assert.Equal(versionId, version.GetProperty("contractVersionId").GetGuid());
            Assert.Equal("Singapore", version.GetProperty("extractedMetadata").GetProperty("governingLaw").GetString());
            Assert.Equal(JsonValueKind.Null, version.GetProperty("editedMetadata").ValueKind);
        }
    }

    [Fact]
    public async Task Metadata_edit_is_kept_next_to_the_extracted_values()
    {
        var (client, _, _) = await Scenario.SignedInUserAsync(factory);
        using (client)
        {
            var (contractId, versionId, _) = await Scenario.AnalyzedContractAsync(factory, client);

            ApiAssert.Status(
                await client.PutAsync($"/contracts/{contractId}/versions/{versionId}/metadata", new
                {
                    parties = new[] { " Bên A ", "", "Bên B" },
                    contractType = "msa",
                    contractValue = 5000,
                    currency = "usd",
                    effectiveDate = "2027-01-01",
                    expiryDate = (string?)null,
                    governingLaw = "Việt Nam"
                }),
                HttpStatusCode.NoContent);

            var version = (await ApiAssert.JsonAsync(await client.GetAsync($"/contracts/{contractId}"))).GetProperty("version");
            var edited = version.GetProperty("editedMetadata");
            Assert.Equal(["Bên A", "Bên B"], edited.GetProperty("parties").EnumerateArray().Select(p => p.GetString()));
            Assert.Equal("USD", edited.GetProperty("currency").GetString());
            Assert.Equal("msa", edited.GetProperty("contractType").GetString());
            Assert.Equal(JsonValueKind.Null, edited.GetProperty("expiryDate").ValueKind);
            Assert.Equal("Singapore", version.GetProperty("extractedMetadata").GetProperty("governingLaw").GetString());

            await ApiAssert.ProblemAsync(
                await client.PutAsync($"/contracts/{contractId}/versions/{versionId}/metadata", new { currency = "dollars" }),
                HttpStatusCode.BadRequest,
                "ERR-GEN-VALIDATION");
        }
    }

    [Fact]
    public async Task Approval_status_follows_the_state_model()
    {
        var (client, _, _) = await Scenario.SignedInUserAsync(factory);
        using (client)
        {
            var (contractId, _, _) = await Scenario.AnalyzedContractAsync(factory, client);

            ApiAssert.Status(await client.PutAsync($"/contracts/{contractId}/approval-status", new { status = "negotiating" }), HttpStatusCode.NoContent);
            ApiAssert.Status(await client.PutAsync($"/contracts/{contractId}/approval-status", new { status = "approved" }), HttpStatusCode.NoContent);

            // FR-CTR-013: approved -> rejected is not in §13.2, the status stays.
            var refused = await client.PutAsync($"/contracts/{contractId}/approval-status", new { status = "rejected" });
            Assert.False(refused.IsSuccessStatusCode);
            Assert.Equal("ERR-CTR-001", (await ApiAssert.JsonAsync(refused)).GetProperty("code").GetString());
            var contract = await ApiAssert.JsonAsync(await client.GetAsync($"/contracts/{contractId}"));
            Assert.Equal("approved", contract.GetProperty("approvalStatus").GetString());
        }
    }

    [Fact]
    public async Task Finding_handling_status_and_note_are_saved()
    {
        var (client, _, _) = await Scenario.SignedInUserAsync(factory);
        using (client)
        {
            var (_, _, jobId) = await Scenario.AnalyzedContractAsync(factory, client);
            var findingId = (await ApiAssert.JsonAsync(await client.GetAsync($"/jobs/{jobId}/result"))).GetProperty("findings")[0].GetProperty("findingId").GetGuid();

            ApiAssert.Status(
                await client.PutAsync($"/findings/{findingId}/handling", new { status = "needs_negotiation", note = "  Hỏi lại bên bán  " }),
                HttpStatusCode.NoContent);

            var finding = (await ApiAssert.JsonAsync(await client.GetAsync($"/jobs/{jobId}/result"))).GetProperty("findings")[0];
            Assert.Equal("needs_negotiation", finding.GetProperty("handlingStatus").GetString());
            Assert.Equal("Hỏi lại bên bán", finding.GetProperty("handlingNote").GetString());
        }
    }

    [Fact]
    public async Task Another_user_cannot_read_or_edit_results()
    {
        var (alice, _, _) = await Scenario.SignedInUserAsync(factory);
        var (bob, _, _) = await Scenario.SignedInUserAsync(factory);
        using (alice)
        using (bob)
        {
            var (contractId, versionId, jobId) = await Scenario.AnalyzedContractAsync(factory, alice);
            var findingId = (await ApiAssert.JsonAsync(await alice.GetAsync($"/jobs/{jobId}/result"))).GetProperty("findings")[0].GetProperty("findingId").GetGuid();

            await ApiAssert.ProblemAsync(await bob.GetAsync($"/jobs/{jobId}/result"), HttpStatusCode.NotFound, "ERR-GEN-NOT-FOUND");
            await ApiAssert.ProblemAsync(await bob.PutAsync($"/findings/{findingId}/handling", new { status = "resolved" }), HttpStatusCode.NotFound, "ERR-GEN-NOT-FOUND");
            await ApiAssert.ProblemAsync(await bob.PutAsync($"/contracts/{contractId}/versions/{versionId}/metadata", new { }), HttpStatusCode.NotFound, "ERR-GEN-NOT-FOUND");
            await ApiAssert.ProblemAsync(await bob.PutAsync($"/contracts/{contractId}/approval-status", new { status = "approved" }), HttpStatusCode.NotFound, "ERR-GEN-NOT-FOUND");
        }
    }
}
