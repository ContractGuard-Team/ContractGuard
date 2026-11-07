using ContractGuard.Application.Abstractions;
using ContractGuard.Application.Analysis;
using ContractGuard.Domain.Analysis;
using ContractGuard.Domain.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace ContractGuard.Application.Contracts;

// Reads for the UI. No workspace condition here: the query filters of the DbContext apply it to
// every query (SD-05), so a contract of another workspace simply looks like it does not exist.
public sealed class ContractQueries(IApplicationDbContext db, IObjectStorage storage, IOptions<AnalysisOptions> analysis)
{
    // shortcut: newest 200 without paging, P5-03 adds paging with search and sort.
    private const int LibraryLimit = 200;

    private static readonly TimeSpan FileLinkLifetime = TimeSpan.FromMinutes(15);

    public Task<List<ContractSummary>> ListAsync(CancellationToken cancellationToken) =>
        db.Contracts
            .OrderByDescending(c => c.UpdatedAt)
            .Take(LibraryLimit)
            .Select(c => new ContractSummary(
                c.ContractId,
                c.ContractName,
                c.ApprovalStatus,
                c.Versions.Max(v => v.VersionNumber),
                c.UpdatedAt))
            .ToListAsync(cancellationToken);

    public async Task<ContractDetail> GetAsync(Guid contractId, CancellationToken cancellationToken)
    {
        var contract = await db.Contracts
            .AsNoTracking()
            .Include(c => c.Versions)
            .ThenInclude(v => v.Context)
            .SingleOrDefaultAsync(c => c.ContractId == contractId, cancellationToken)
            ?? throw NotFound("Contract");

        var version = contract.LatestVersion;
        var runs = db.AnalysisRuns.Where(r => r.ContractVersionId == version.ContractVersionId);
        var latestJob = await runs
            .OrderByDescending(r => r.StartedAt)
            .Select(r => ToJobStatus(r))
            .FirstOrDefaultAsync(cancellationToken);
        var completedJobId = await runs
            .Where(r => r.RunStatus == RunStatus.Completed)
            .OrderByDescending(r => r.StartedAt)
            .Select(r => (Guid?)r.AnalysisRunId)
            .FirstOrDefaultAsync(cancellationToken);

        return new ContractDetail(
            contract.ContractId,
            contract.ContractName,
            contract.ApprovalStatus,
            new ContractVersionDetail(
                version.ContractVersionId,
                version.VersionNumber,
                version.OriginalFileName,
                version.FileType,
                version.FileSizeBytes,
                version.PageCount,
                version.Context?.ToDetails()),
            latestJob,
            completedJobId,
            analysis.Value.WorkerEnabled);
    }

    public async Task<ContractFileLink> GetFileLinkAsync(Guid contractId, Guid contractVersionId, CancellationToken cancellationToken)
    {
        var file = await db.ContractVersions
            .Where(v => v.ContractId == contractId && v.ContractVersionId == contractVersionId)
            .Select(v => new { v.SourceFileKey, v.OriginalFileName, v.FileType })
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw NotFound("Contract version");

        // Checked here because the browser would otherwise open the storage's own XML error page.
        if (await storage.GetSizeAsync(file.SourceFileKey, cancellationToken) is null)
        {
            throw NotFound("Contract file");
        }

        // The type comes from the checked upload, not from the object, so a file can only be served as what it passed as.
        var url = await storage.CreateDownloadUrlAsync(
            file.SourceFileKey,
            file.OriginalFileName,
            FileTypeDetector.ContentTypeOf(file.FileType),
            FileLinkLifetime,
            cancellationToken);
        return new ContractFileLink(url);
    }

    public async Task<JobStatus> GetJobAsync(Guid jobId, CancellationToken cancellationToken) =>
        await db.AnalysisRuns
            .Where(r => r.AnalysisRunId == jobId)
            .Select(r => ToJobStatus(r))
            .SingleOrDefaultAsync(cancellationToken)
        ?? throw NotFound("Job");

    public async Task<List<ClauseItem>> GetClausesAsync(Guid jobId, CancellationToken cancellationToken)
    {
        if (!await db.AnalysisRuns.AnyAsync(r => r.AnalysisRunId == jobId, cancellationToken))
        {
            throw NotFound("Job");
        }

        return await db.Clauses
            .Where(c => c.AnalysisRunId == jobId)
            .OrderBy(c => c.ClauseOrder)
            .Select(c => new ClauseItem(c.ClauseId, c.ClauseKey, c.ClauseOrder, c.ClauseNumber, c.Heading, c.ClauseText, c.ClauseType, c.Location))
            .ToListAsync(cancellationToken);
    }

    private static JobStatus ToJobStatus(AnalysisRun r) =>
        new(r.AnalysisRunId, r.ContractId, r.ContractVersionId, r.RunStatus, r.CurrentStep, r.FailureReason, r.StartedAt, r.FinishedAt);

    private static DomainException NotFound(string what) => new(ErrorCodes.NotFound, $"{what} not found.");
}
