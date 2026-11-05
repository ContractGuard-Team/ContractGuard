using System.Text.Json;
using ContractGuard.Application.Analysis.Extraction;
using ContractGuard.Domain.Contracts;
using ContractGuard.Infrastructure.Messaging;

namespace ContractGuard.UnitTests;

internal static class TestData
{
    public static readonly DateTimeOffset Now = new(2026, 11, 2, 9, 0, 0, TimeSpan.Zero);

    public static ContractFile PdfFile(string key = "quarantine/test.pdf") => new(key, "contract.pdf", FileType.Pdf, 1_000);

    public static ContextDetails Context() =>
        new(OwnParty.Buyer, ContractType.ServiceAgreement, HasPersonalData: true, "Vendor", "Cloud hosting", ServiceCriticality.High);

    // A contract whose V1 has a confirmed context, ready to be analyzed.
    public static Contract ReadyContract()
    {
        var contract = Contract.Create("Test contract", null, Guid.CreateVersion7(), PdfFile(), Now);
        contract.ConfirmContext(contract.LatestVersion.ContractVersionId, Context(), Now);
        return contract;
    }

    // contracts/examples/extraction.v2.example.json with real ids instead of the placeholders.
    public static string ExampleExtractionJson(Guid jobId, Guid contractId, int version = 1) =>
        File.ReadAllText(RepoFile("contracts", "examples", "extraction.v2.example.json"))
            .Replace("{{JOB_ID}}", jobId.ToString(), StringComparison.Ordinal)
            .Replace("{{TENANT_ID}}", "test", StringComparison.Ordinal)
            .Replace("{{CONTRACT_ID}}", contractId.ToString(), StringComparison.Ordinal)
            .Replace("{{CONTRACT_VERSION}}", version.ToString(System.Globalization.CultureInfo.InvariantCulture), StringComparison.Ordinal);

    public static ExtractionDocument ExampleExtraction(Guid jobId, Guid contractId, int version = 1) =>
        JsonSerializer.Deserialize<ExtractionDocument>(ExampleExtractionJson(jobId, contractId, version), MessageJson.Options)!;

    // Finds a file of the repository by walking up from the test binary folder.
    public static string RepoFile(params string[] parts)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !Directory.Exists(Path.Combine(directory.FullName, "contracts")))
        {
            directory = directory.Parent;
        }

        return directory is null
            ? throw new DirectoryNotFoundException("Repository root (folder with 'contracts') not found.")
            : Path.Combine([directory.FullName, .. parts]);
    }
}
