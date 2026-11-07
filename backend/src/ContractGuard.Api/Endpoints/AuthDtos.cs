using ContractGuard.Domain.Users;
using ContractGuard.Domain.Workspaces;

namespace ContractGuard.Api.Endpoints;

// Request and response bodies of AuthEndpoints, kept together because they are read as one API.
internal sealed record CsrfTokenResponse(string Token);

internal sealed record RegisterRequest(string DisplayName, string Email, string Password);

internal sealed record VerifyEmailRequest(Guid UserId, string Token);

internal sealed record EmailRequest(string Email);

internal sealed record LoginRequest(string Email, string Password);

internal sealed record ResetPasswordRequest(Guid UserId, string Token, string NewPassword);

internal sealed record MeResponse(Guid UserId, string DisplayName, string Email, UiLanguage UiLanguage, IReadOnlyList<WorkspaceResponse> Workspaces);

// OrganizationId is null for the personal workspace of the user (FR-ORG-013).
internal sealed record WorkspaceResponse(Guid? OrganizationId, string? OrganizationName, OrgRole? Role);
