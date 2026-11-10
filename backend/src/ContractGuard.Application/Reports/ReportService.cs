using ContractGuard.Application.Abstractions;
using ContractGuard.Application.Contracts;
using ContractGuard.Domain.Analysis;
using ContractGuard.Domain.Common;
using ContractGuard.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ContractGuard.Application.Reports;

// F-EXP-001, F-EXP-002: builds the report of one completed run and renders it. Generated on demand and
// never stored: the data can change (handling status, metadata) and a stored copy would outlive a deletion.
public sealed partial class ReportService(
    IApplicationDbContext db,
    ContractQueries queries,
    IEnumerable<IReportRenderer> renderers,
    AuditLog audit,
    TimeProvider clock,
    ILogger<ReportService> logger)
{
    public async Task<ReportFile> ExportAsync(Guid jobId, ReportFormat format, UiLanguage language, ReportScope scope, CancellationToken cancellationToken)
    {
        var data = await BuildAsync(jobId, language, scope, cancellationToken);
        var renderer = renderers.Single(r => r.Format == format);
        byte[] bytes;
        try
        {
            bytes = renderer.Render(data);
        }
        catch (Exception ex)
        {
            // FR-EXP-004: the user is told that the report failed; the details stay in the log.
            LogRenderFailed(ex, jobId, format);
            throw new DomainException(ErrorCodes.ExportFailed, "The report could not be generated.");
        }

        var contractId = await db.AnalysisRuns.Where(r => r.AnalysisRunId == jobId).Select(r => r.ContractId).SingleAsync(cancellationToken);
        audit.Record(contractId, Domain.Contracts.AuditAction.ReportExported, $"{format} {language} {scope}");
        await db.SaveChangesAsync(cancellationToken);

        var extension = format == ReportFormat.Pdf ? "pdf" : "docx";
        var fileName = $"{SafeFileName(data.ContractName)}-V{data.VersionNumber}-report.{extension}";
        return new ReportFile(bytes, renderer.ContentType, fileName);
    }

    public async Task<ReportData> BuildAsync(Guid jobId, UiLanguage language, ReportScope scope, CancellationToken cancellationToken)
    {
        var result = await queries.GetResultAsync(jobId, cancellationToken); // 404 when not completed or not visible.
        var run = await db.AnalysisRuns.AsNoTracking().SingleAsync(r => r.AnalysisRunId == jobId, cancellationToken);
        var contract = await db.Contracts
            .AsNoTracking()
            .Include(c => c.Versions.Where(v => v.ContractVersionId == run.ContractVersionId))
            .ThenInclude(v => v.Context)
            .SingleAsync(c => c.ContractId == run.ContractId, cancellationToken);
        var version = contract.Versions.Single();
        var clauseNames = await db.Clauses
            .Where(c => c.AnalysisRunId == jobId)
            .Select(c => new { c.ClauseKey, c.ClauseNumber, c.Heading })
            .ToDictionaryAsync(c => c.ClauseKey, c => string.Join(". ", new[] { c.ClauseNumber, c.Heading }.Where(s => !string.IsNullOrWhiteSpace(s))), cancellationToken);

        var findings = scope == ReportScope.Severe
            ? result.Findings.Where(f => f.Severity is Severity.High or Severity.Critical).ToList()
            : result.Findings;

        // Counted over all findings, whatever the scope: the summary describes the whole contract.
        var counts = Enum.GetValues<Severity>().ToDictionary(s => s, s => result.Findings.Count(f => f.Severity == s));

        return new ReportData(
            ReportText.For(language),
            contract.ContractName,
            version.VersionNumber,
            version.OriginalFileName,
            contract.ApprovalStatus,
            version.Context?.ToDetails(),
            version.Metadata,
            result.Score,
            result.OverallRecommendation,
            counts,
            findings,
            result.Checks,
            clauseNames,
            result.Trace,
            scope,
            clock.GetUtcNow());
    }

    private static string SafeFileName(string name)
    {
        var invalid = Path.GetInvalidFileNameChars().Concat(['"', ' ']).ToHashSet();
        var safe = new string([.. name.Select(c => invalid.Contains(c) ? '-' : c)]).Trim('-');
        return safe.Length == 0 ? "contract" : safe.Length > 80 ? safe[..80] : safe;
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "Report of job {JobId} could not be rendered as {Format}")]
    private partial void LogRenderFailed(Exception ex, Guid jobId, ReportFormat format);
}
