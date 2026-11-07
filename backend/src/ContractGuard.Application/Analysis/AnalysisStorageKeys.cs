namespace ContractGuard.Application.Analysis;

// S3 key layout of SD-07, in one place.
public static class AnalysisStorageKeys
{
    public static string ResultsPrefix(Guid jobId) => $"results/{jobId}/";

    // Job details written by the backend for the worker (contracts/analysis-request.v1.schema.json).
    public static string RequestKey(Guid jobId) => $"{ResultsPrefix(jobId)}request.json";

    // Where the worker must write its extraction.v2 document.
    public static string ExtractionKey(Guid jobId) => $"{ResultsPrefix(jobId)}extraction.json";

    // Workspace segment: the organization id, or "personal-<user id>" for a personal contract.
    public static string WorkspaceSegment(Guid? organizationId, Guid ownerUserAccountId) =>
        organizationId is { } org ? org.ToString() : $"personal-{ownerUserAccountId}";

    // The user id in the key ties an upload to its uploader.
    public static string QuarantineKey(Guid userId, Guid uploadId, string extension) => $"quarantine/{userId}/{uploadId}{extension}";

    public static string CleanKey(string workspace, Guid contractId, int versionNumber, string extension) =>
        $"clean/{workspace}/{contractId}/v{versionNumber}/source{extension}";

    public static string ContractPrefix(string workspace, Guid contractId) => $"clean/{workspace}/{contractId}/";
}
