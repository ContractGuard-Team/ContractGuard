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

// HasPassword is false for an account that has only ever signed in with Google (FR-AUTH-017).
internal sealed record MeResponse(
    Guid UserId,
    string DisplayName,
    string Email,
    string? PhoneNumber,
    UiLanguage UiLanguage,
    bool HasPassword,
    IReadOnlyList<WorkspaceResponse> Workspaces);

// FR-AUTH-016 (CR-AUTH-001). An empty phone number removes it.
internal sealed record ProfileRequest(string DisplayName, string? PhoneNumber, UiLanguage UiLanguage);

// FR-AUTH-017 (CR-AUTH-001). CurrentPassword is left out when the account has none yet.
internal sealed record ChangePasswordRequest(string? CurrentPassword, string NewPassword);

// OrganizationId is null for the personal workspace of the user (FR-ORG-013).
internal sealed record WorkspaceResponse(Guid? OrganizationId, string? OrganizationName, OrgRole? Role);
