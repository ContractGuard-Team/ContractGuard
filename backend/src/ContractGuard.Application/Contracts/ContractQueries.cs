using ContractGuard.Application.Abstractions;
using ContractGuard.Application.Analysis;
using ContractGuard.Domain.Analysis;
using ContractGuard.Domain.Common;
using ContractGuard.Domain.Contracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace ContractGuard.Application.Contracts;

// Reads for the UI. No workspace condition here: the query filters of the DbContext apply it to
// every query (SD-05), so a contract of another workspace simply looks like it does not exist.
public sealed class ContractQueries(
    IApplicationDbContext db,
    IObjectStorage storage,
    ICurrentWorkspace workspace,
    IOptions<AnalysisOptions> analysis,
    IWorkerActivityReader workerActivity)
{
    public const int MaxPageSize = 50;

    private static readonly TimeSpan FileLinkLifetime = TimeSpan.FromMinutes(15);

    // F-CTR-003: FR-CTR-008 (what the filters let through), 009 search, 010 filters, 011 sort, plus paging.
    // Risk, counterparty and type are those of the latest version and its newest completed analysis.
    public async Task<LibraryPage> ListAsync(LibraryQuery query, CancellationToken cancellationToken)
    {
        var page = Math.Max(query.Page, 1);
        var pageSize = Math.Clamp(query.PageSize, 1, MaxPageSize);
        var me = workspace.UserId;

        var rows =
            from c in db.Contracts
            let v = c.Versions.OrderByDescending(v => v.VersionNumber).First()
            let run = db.AnalysisRuns
                .Where(r => r.ContractVersionId == v.ContractVersionId && r.RunStatus == RunStatus.Completed)
                .OrderByDescending(r => r.StartedAt)
                .FirstOrDefault()
            select new
            {
                c.ContractId,
                c.ContractName,
                c.ApprovalStatus,
                c.UpdatedAt,
                c.OwnerUserAccountId,
                v.VersionNumber,
                Counterparty = v.Context != null ? v.Context.CounterpartyName : null,
                Type = v.Context != null ? (ContractType?)v.Context.ContractType : null,
                Score = run != null ? run.OverallScore : null,
                Level = run != null ? run.OverallRiskLevel : null
            };

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim().ToLower();
            rows = rows.Where(x => x.ContractName.ToLower().Contains(term) || (x.Counterparty != null && x.Counterparty.ToLower().Contains(term)));
        }

        if (query.RiskLevel is { } level)
        {
            rows = rows.Where(x => x.Level == level);
        }

        if (query.Status is { } status)
        {
            rows = rows.Where(x => x.ApprovalStatus == status);
        }

        if (query.ContractType is { } type)
        {
            rows = rows.Where(x => x.Type == type);
        }

        var total = await rows.CountAsync(cancellationToken);
        rows = (query.Sort, query.Descending) switch
        {
            (LibrarySort.Name, false) => rows.OrderBy(x => x.ContractName).ThenBy(x => x.ContractId),
            (LibrarySort.Name, true) => rows.OrderByDescending(x => x.ContractName).ThenBy(x => x.ContractId),
            // Contracts without a score go last whichever the direction.
            (LibrarySort.Score, false) => rows.OrderBy(x => x.Score == null).ThenBy(x => x.Score).ThenBy(x => x.ContractId),
            (LibrarySort.Score, true) => rows.OrderBy(x => x.Score == null).ThenByDescending(x => x.Score).ThenBy(x => x.ContractId),
            // Lifecycle order of SRS 13 rather than alphabetical.
            (LibrarySort.Status, false) => rows.OrderBy(x => x.ApprovalStatus == ApprovalStatus.New ? 0 : x.ApprovalStatus == ApprovalStatus.Analyzing ? 1 : x.ApprovalStatus == ApprovalStatus.PendingReview ? 2 : x.ApprovalStatus == ApprovalStatus.Negotiating ? 3 : x.ApprovalStatus == ApprovalStatus.Approved ? 4 : 5).ThenByDescending(x => x.UpdatedAt),
            (LibrarySort.Status, true) => rows.OrderByDescending(x => x.ApprovalStatus == ApprovalStatus.New ? 0 : x.ApprovalStatus == ApprovalStatus.Analyzing ? 1 : x.ApprovalStatus == ApprovalStatus.PendingReview ? 2 : x.ApprovalStatus == ApprovalStatus.Negotiating ? 3 : x.ApprovalStatus == ApprovalStatus.Approved ? 4 : 5).ThenByDescending(x => x.UpdatedAt),
            (_, false) => rows.OrderBy(x => x.UpdatedAt).ThenBy(x => x.ContractId),
            _ => rows.OrderByDescending(x => x.UpdatedAt).ThenBy(x => x.ContractId)
        };

        var items = await rows
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new ContractSummary(x.ContractId, x.ContractName, x.ApprovalStatus, x.VersionNumber, x.UpdatedAt, x.Counterparty, x.Type, x.Score, x.Level, x.OwnerUserAccountId != me))
            .ToListAsync(cancellationToken);
        return new LibraryPage(items, total, page, pageSize);
    }

    // FR-CTR-016: any version can be opened; without a version id the latest one is shown.
    public async Task<ContractDetail> GetAsync(Guid contractId, Guid? contractVersionId, CancellationToken cancellationToken)
    {
        var contract = await db.Contracts
            .AsNoTracking()
            .Include(c => c.Versions)
            .ThenInclude(v => v.Context)
            .SingleOrDefaultAsync(c => c.ContractId == contractId, cancellationToken)
            ?? throw NotFound("Contract");

        var version = contractVersionId is { } id ? contract.GetVersion(id) : contract.LatestVersion;
        // CR-ANA-002: a stopped run is as if it never started; the page goes back to how it was before it.
        var runs = db.AnalysisRuns.Where(r => r.ContractVersionId == version.ContractVersionId && r.RunStatus != RunStatus.Cancelled);
        var latestJob = await runs
            .OrderByDescending(r => r.StartedAt)
            .Select(r => ToJobStatus(r))
            .FirstOrDefaultAsync(cancellationToken);
        var completedJobId = await runs
            .Where(r => r.RunStatus == RunStatus.Completed)
            .OrderByDescending(r => r.StartedAt)
            .Select(r => (Guid?)r.AnalysisRunId)
            .FirstOrDefaultAsync(cancellationToken);

        var versionIds = contract.Versions.Select(v => v.ContractVersionId).ToList();
        var allRuns = await db.AnalysisRuns
            .Where(r => versionIds.Contains(r.ContractVersionId) && r.RunStatus != RunStatus.Cancelled)
            .Select(r => new { r.ContractVersionId, r.RunStatus, r.OverallScore, r.OverallRiskLevel, r.StartedAt })
            .ToListAsync(cancellationToken);
        var versions = contract.Versions
            .OrderByDescending(v => v.VersionNumber)
            .Select(v =>
            {
                var run = allRuns.Where(r => r.ContractVersionId == v.ContractVersionId).MaxBy(r => r.StartedAt);
                return new VersionItem(v.ContractVersionId, v.VersionNumber, v.OriginalFileName, v.UploadedAt, run?.RunStatus, run?.OverallScore, run?.OverallRiskLevel);
            })
            .ToList();

        // FR-CTR-015: a new version without its own context starts from the previous version's.
        var suggestedContext = version.Context is null
            ? contract.Versions.Where(v => v.VersionNumber < version.VersionNumber && v.Context is not null).MaxBy(v => v.VersionNumber)?.Context?.ToDetails()
            : null;

        return new ContractDetail(
            contract.ContractId,
            contract.ContractName,
            contract.ApprovalStatus,
            contract.OrganizationId,
            ContractAccess.IsOwner(contract, workspace) ? ContractRole.Owner : ContractRole.Member,
            new ContractVersionDetail(
                version.ContractVersionId,
                version.VersionNumber,
                version.OriginalFileName,
                version.FileType,
                version.FileSizeBytes,
                version.PageCount,
                version.Context?.ToDetails(),
                version.ExtractedMetadata,
                version.EditedMetadata),
            version.ContractVersionId == contract.LatestVersion.ContractVersionId,
            suggestedContext,
            versions,
            latestJob,
            completedJobId,
            analysis.Value.WorkerEnabled);
    }

    // CR-CTR-001: the trash of the current workspace. Like delete and restore it is Owner only (ContractAccess.IsOwner):
    // a member a contract was shared with loses it from view and does not see it here.
    public async Task<List<TrashItem>> ListTrashAsync(CancellationToken cancellationToken)
    {
        var me = workspace.UserId;
        var organizationOwner = workspace.OrganizationId is not null && workspace.IsOrganizationOwner;
        var rows = await db.Contracts.IgnoreQueryFilters([QueryFilters.NotDeleted])
            .Where(c => c.DeletedAt != null && (c.OwnerUserAccountId == me || (organizationOwner && c.OrganizationId == workspace.OrganizationId)))
            .OrderByDescending(c => c.DeletedAt)
            .Select(c => new
            {
                c.ContractId,
                c.ContractName,
                OriginalFileName = c.Versions.OrderByDescending(v => v.VersionNumber).Select(v => v.OriginalFileName).First(),
                DeletedAt = c.DeletedAt!.Value
            })
            .ToListAsync(cancellationToken);
        return rows.Select(r => new TrashItem(r.ContractId, r.ContractName, r.OriginalFileName, r.DeletedAt, r.DeletedAt + Contract.TrashRetention)).ToList();
    }

    public async Task<ContractFileLink> GetFileLinkAsync(Guid contractId, Guid contractVersionId, CancellationToken cancellationToken)
    {
        var file = await db.ContractVersions
            .Where(v => v.ContractId == contractId && v.ContractVersionId == contractVersionId)
            .Join(db.Contracts, v => v.ContractId, c => c.ContractId, (v, c) => new
            {
                v.SourceFileKey,
                v.OriginalFileName,
                v.FileType,
                v.VersionNumber,
                c.OrganizationId,
                c.OwnerUserAccountId
            })
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw NotFound("Contract version");

        // The worker deletes the quarantine file once it reports `completed`; if the result was then rejected
        // the version still points there, while the worker's checked copy is in clean/.
        var key = file.SourceFileKey;
        if (await storage.GetSizeAsync(key, cancellationToken) is null)
        {
            key = AnalysisStorageKeys.CleanKey(
                AnalysisStorageKeys.WorkspaceSegment(file.OrganizationId, file.OwnerUserAccountId),
                contractId,
                file.VersionNumber,
                Path.GetExtension(file.OriginalFileName).ToLowerInvariant());

            // Checked here because the browser would otherwise open the storage's own XML error page.
            if (await storage.GetSizeAsync(key, cancellationToken) is null)
            {
                throw NotFound("Contract file");
            }
        }

        // The type comes from the checked upload, not from the object, so a file can only be served as what it passed as.
        var url = await storage.CreateDownloadUrlAsync(
            key,
            file.OriginalFileName,
            FileTypeDetector.ContentTypeOf(file.FileType),
            FileLinkLifetime,
            cancellationToken);
        return new ContractFileLink(url);
    }

    // The page polls this every 3 seconds while a run is going; the worker's finer progress is added then (docs/04 C8).
    public async Task<JobStatus> GetJobAsync(Guid jobId, CancellationToken cancellationToken)
    {
        var job = await db.AnalysisRuns
            .Where(r => r.AnalysisRunId == jobId)
            .Select(r => ToJobStatus(r))
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw NotFound("Job");

        return job.Status == RunStatus.Running
            ? job with { Activity = await workerActivity.ReadAsync(job.ContractId, job.JobId, cancellationToken) }
            : job;
    }

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

    public async Task<AnalysisResultView> GetResultAsync(Guid jobId, CancellationToken cancellationToken)
    {
        var run = await db.AnalysisRuns
            .AsNoTracking()
            .Where(r => r.AnalysisRunId == jobId && r.RunStatus == RunStatus.Completed)
            .Select(r => new { r.SchemaVersion, r.ModelVersion, r.PromptVersion, r.RuleVersion, r.Score, r.OverallRecommendation, r.Checks, r.InjectionWarnings })
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw NotFound("Analysis result");

        var findings = await db.Findings
            .AsNoTracking()
            .Where(f => f.AnalysisRunId == jobId)
            .OrderByDescending(f => f.Severity == Severity.Critical)
            .ThenByDescending(f => f.Severity == Severity.High)
            .ThenByDescending(f => f.Severity == Severity.Medium)
            .ThenBy(f => f.FindingKey)
            .Select(f => new FindingItem(
                f.FindingId, f.FindingKey, f.Kind, f.Title, f.PrimaryDomain, f.SecondaryDomains, f.Category, f.Severity,
                f.ClauseKey, f.Evidence, f.MissingDescription, f.Why, f.Impact, f.Recommendation, f.References, f.CheckCode,
                f.HandlingStatus, f.HandlingNote, f.HandledAt))
            .ToListAsync(cancellationToken);

        return new AnalysisResultView(
            jobId,
            new AnalysisTrace(run.SchemaVersion ?? string.Empty, run.ModelVersion, run.PromptVersion, run.RuleVersion),
            run.Score,
            run.OverallRecommendation,
            findings,
            run.Checks,
            run.InjectionWarnings);
    }

    private static JobStatus ToJobStatus(AnalysisRun r) =>
        new(r.AnalysisRunId, r.ContractId, r.ContractVersionId, r.RunStatus, r.CurrentStep, r.FailureReason, r.StartedAt, r.FinishedAt);

    private static DomainException NotFound(string what) => new(ErrorCodes.NotFound, $"{what} not found.");
}
