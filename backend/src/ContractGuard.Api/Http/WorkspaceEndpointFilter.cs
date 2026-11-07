using System.Security.Claims;
using ContractGuard.Application.Abstractions;
using ContractGuard.Domain.Common;
using ContractGuard.Domain.Workspaces;
using ContractGuard.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ContractGuard.Api.Http;

// Sets the workspace of a request that has a session, before any contract data is read.
// No X-Workspace-Id header means the personal workspace. Membership is read from the database on
// every request, so removing someone from an organization takes effect at once (SEC-AUTHZ-008).
internal sealed partial class WorkspaceEndpointFilter(
    CurrentWorkspace workspace,
    IApplicationDbContext db,
    ILogger<WorkspaceEndpointFilter> logger) : IEndpointFilter
{
    public const string HeaderName = "X-Workspace-Id";

    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var http = context.HttpContext;
        var userId = Guid.Parse(http.User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        // Every log line of this request carries user_id (SD-19).
        using var scope = logger.BeginScope(new Dictionary<string, object> { ["user_id"] = userId });

        if (!http.Request.Headers.TryGetValue(HeaderName, out var header))
        {
            workspace.SetPersonal(userId);
            return await next(context);
        }

        if (!Guid.TryParse(header, out var organizationId))
        {
            throw new DomainException(ErrorCodes.Validation, $"{HeaderName} must be a UUID.");
        }

        var role = await db.Memberships
            .Where(m => m.OrganizationId == organizationId && m.UserAccountId == userId)
            .Select(m => (OrgRole?)m.OrgRole)
            .SingleOrDefaultAsync(http.RequestAborted);
        if (role is null)
        {
            LogNotAMember(organizationId);
            throw new DomainException(ErrorCodes.Forbidden, "You are not a member of this organization.");
        }

        workspace.SetOrganization(userId, organizationId, role == OrgRole.Owner);
        return await next(context);
    }

    // Security event (ERR-SEC-001).
    [LoggerMessage(Level = LogLevel.Warning, Message = "Access to organization {OrganizationId} refused: not a member")]
    private partial void LogNotAMember(Guid organizationId);
}
