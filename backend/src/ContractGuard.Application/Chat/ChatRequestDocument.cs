using ContractGuard.Domain.Analysis;
using ContractGuard.Domain.Chat;
using ContractGuard.Domain.Users;

namespace ContractGuard.Application.Chat;

// C# mirror of contracts/chat-request.v1.schema.json: one chat question the backend writes to
// results/{job_id}/request.json for the worker. It carries only data of one analysis run (BR-CHAT-001).
public sealed record ChatRequestDocument(
    string SchemaVersion,
    Guid JobId,
    Guid AnalysisJobId,
    Guid ContractId,
    Guid ContractVersionId,
    UiLanguage OutputLanguage,
    string Question,
    IReadOnlyList<ChatRequestTurn> History,
    IReadOnlyList<ChatRequestClause> Clauses,
    IReadOnlyList<ChatRequestFinding> Findings,
    string? OverallRecommendation);

public sealed record ChatRequestTurn(ChatRole Role, string Content);

// Field names follow the clauses of extraction.v2.
public sealed record ChatRequestClause(string ClauseId, string Number, string? Heading, string Text, int PageStart, int PageEnd);

public sealed record ChatRequestFinding(string FindingId, string Title, Severity Severity, string? ClauseId, string Why, string Impact);
