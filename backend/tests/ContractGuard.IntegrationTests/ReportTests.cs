using System.Net;
using DocumentFormat.OpenXml.Packaging;

namespace ContractGuard.IntegrationTests;

// F-EXP-001, F-EXP-002: the report files, rendered for real from the example result.
[Collection(ApiCollection.Name)]
public sealed class ReportTests(ApiFactory factory)
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Pdf_report_is_a_pdf_named_after_the_contract()
    {
        var (client, _, _) = await Scenario.SignedInUserAsync(factory);
        using (client)
        {
            var (_, _, jobId) = await Scenario.AnalyzedContractAsync(factory, client);

            var response = await client.GetAsync($"/jobs/{jobId}/report?format=pdf");

            ApiAssert.Status(response, HttpStatusCode.OK);
            Assert.Equal("application/pdf", response.Content.Headers.ContentType?.MediaType);
            Assert.EndsWith("-V1-report.pdf", response.Content.Headers.ContentDisposition?.FileNameStar, StringComparison.Ordinal);
            var bytes = await response.Content.ReadAsByteArrayAsync(Ct);
            Assert.Equal("%PDF"u8.ToArray(), bytes[..4]);
        }
    }

    [Fact]
    public async Task Docx_report_has_every_section_of_FR_EXP_002()
    {
        var (client, _, _) = await Scenario.SignedInUserAsync(factory);
        using (client)
        {
            var (_, _, jobId) = await Scenario.AnalyzedContractAsync(factory, client);

            var text = await DocxTextAsync(client, $"/jobs/{jobId}/report?format=docx");

            Assert.Contains("Thông tin hợp đồng", text, StringComparison.Ordinal);
            Assert.Contains("72/100", text, StringComparison.Ordinal);
            Assert.Contains("Điểm theo nhóm rủi ro", text, StringComparison.Ordinal);
            Assert.Contains("Không áp dụng", text, StringComparison.Ordinal);
            Assert.Contains("Có 1 rủi ro Nghiêm trọng và 2 rủi ro Cao", text, StringComparison.Ordinal);
            Assert.Contains("Không có quyền kiểm toán bảo mật", text, StringComparison.Ordinal);
            Assert.Contains("In no event shall Provider be liable", text, StringComparison.Ordinal);
            Assert.Contains("Checklist 20 check", text, StringComparison.Ordinal);
            Assert.Contains("không phải ý kiến pháp lý", text, StringComparison.Ordinal);
        }
    }

    [Fact]
    public async Task English_report_with_severe_scope_leaves_out_medium_findings()
    {
        var (client, _, _) = await Scenario.SignedInUserAsync(factory);
        using (client)
        {
            var (_, _, jobId) = await Scenario.AnalyzedContractAsync(factory, client);

            var text = await DocxTextAsync(client, $"/jobs/{jobId}/report?format=docx&language=en&scope=severe");

            Assert.Contains("Contract risk review report", text, StringComparison.Ordinal);
            Assert.Contains("only lists High and Critical", text, StringComparison.Ordinal);
            Assert.DoesNotContain("Không có quyền kiểm toán bảo mật", text, StringComparison.Ordinal);
            Assert.Contains("Sự cố bảo mật bị loại trừ", text, StringComparison.Ordinal);
        }
    }

    [Fact]
    public async Task Report_of_a_run_without_result_or_of_someone_else_is_not_found()
    {
        var (alice, _, _) = await Scenario.SignedInUserAsync(factory);
        var (bob, _, _) = await Scenario.SignedInUserAsync(factory);
        using (alice)
        using (bob)
        {
            var (contractId, versionId) = await Scenario.UploadContractAsync(factory, alice);
            var running = await Scenario.StartAnalysisAsync(alice, contractId, versionId);
            await ApiAssert.ProblemAsync(await alice.GetAsync($"/jobs/{running}/report"), HttpStatusCode.NotFound, "ERR-GEN-NOT-FOUND");

            await Scenario.CompleteJobAsync(factory, running, contractId);
            await ApiAssert.ProblemAsync(await bob.GetAsync($"/jobs/{running}/report"), HttpStatusCode.NotFound, "ERR-GEN-NOT-FOUND");
        }
    }

    private static async Task<string> DocxTextAsync(ApiClient client, string path)
    {
        var response = await client.GetAsync(path);
        ApiAssert.Status(response, HttpStatusCode.OK);
        using var stream = new MemoryStream(await response.Content.ReadAsByteArrayAsync(Ct));
        using var document = WordprocessingDocument.Open(stream, false);
        return document.MainDocumentPart!.Document!.Body!.InnerText;
    }
}
