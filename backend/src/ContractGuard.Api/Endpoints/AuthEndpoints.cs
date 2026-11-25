using System.Security.Claims;
using System.Text.RegularExpressions;
using ContractGuard.Application.Abstractions;
using ContractGuard.Api.Http;
using ContractGuard.Domain.Common;
using ContractGuard.Infrastructure.Identity;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ContractGuard.Api.Endpoints;

// F-AUTH-001: registration, email verification, session, password reset.
internal static partial class AuthEndpoints
{
    private const int EmailMaxLength = 256; // Column length of the Identity tables.

    // Open to everyone (SEC-AUTH-001 lists them as the exceptions).
    public static IEndpointRouteBuilder MapPublicAuthEndpoints(this IEndpointRouteBuilder api)
    {
        var auth = api.MapGroup("/auth").WithTags("Auth");
        auth.MapGet("/csrf", GetCsrfToken);
        auth.MapPost("/register", Register);
        auth.MapPost("/verify-email", VerifyEmail);
        auth.MapPost("/resend-verification", ResendVerification);
        auth.MapPost("/login", Login).RequireRateLimiting(RateLimits.Login);
        auth.MapPost("/forgot-password", ForgotPassword).RequireRateLimiting(RateLimits.PasswordReset);
        auth.MapPost("/reset-password", ResetPassword).RequireRateLimiting(RateLimits.PasswordReset);
        auth.MapGoogleLogin(); // F-AUTH-002
        return api;
    }

    // Need a valid session.
    public static IEndpointRouteBuilder MapAccountEndpoints(this IEndpointRouteBuilder secured)
    {
        secured.MapPost("/auth/logout", Logout).WithTags("Auth");
        secured.MapGet("/me", GetMe).WithTags("Auth");
        secured.MapPut("/me/profile", UpdateProfile).WithTags("Auth");
        // Rate limited: a wrong current password does not count toward the account lockout.
        secured.MapPost("/me/password", ChangePassword).WithTags("Auth").RequireRateLimiting(RateLimits.Login);
        return secured;
    }

    private static Ok<CsrfTokenResponse> GetCsrfToken(HttpContext http, IAntiforgery antiforgery) =>
        TypedResults.Ok(new CsrfTokenResponse(antiforgery.GetAndStoreTokens(http).RequestToken!));

    private static async Task<NoContent> Register(
        RegisterRequest request,
        UserManager<AppUser> users,
        AccountEmails emails,
        CancellationToken cancellationToken)
    {
        var displayName = ValidDisplayName(request.DisplayName);
        var email = NormalizeEmail(request.Email);
        var user = new AppUser { Id = Guid.CreateVersion7(), DisplayName = displayName, UserName = email, Email = email };
        ThrowIfFailed(await users.CreateAsync(user, request.Password));

        // If this throws ERR-INTG-001 the account stays; the user asks for the email again.
        await emails.SendVerificationAsync(user, cancellationToken);
        return TypedResults.NoContent();
    }

    private static async Task<NoContent> VerifyEmail(VerifyEmailRequest request, UserManager<AppUser> users)
    {
        var user = await users.FindByIdAsync(request.UserId.ToString());
        if (user is null)
        {
            throw LinkExpired();
        }

        // Opening the same link again is not an error for someone already verified.
        if (!user.EmailConfirmed && !(await users.ConfirmEmailAsync(user, request.Token)).Succeeded)
        {
            throw LinkExpired();
        }

        return TypedResults.NoContent();
    }

    // Same limits as the password reset email (FR-AUTH-014), same answer whether sent or not.
    private static async Task<NoContent> ResendVerification(
        EmailRequest request,
        UserManager<AppUser> users,
        AccountEmails emails,
        TimeProvider clock,
        CancellationToken cancellationToken)
    {
        var user = await users.FindByEmailAsync(NormalizeEmail(request.Email));
        if (user is { EmailConfirmed: false } && EmailThrottle.TryRecord(user.VerificationEmailsSentAt, clock.GetUtcNow()) is { } sent)
        {
            user.VerificationEmailsSentAt = sent;
            await users.UpdateAsync(user);
            await emails.SendVerificationAsync(user, cancellationToken);
        }

        return TypedResults.NoContent();
    }

    private static async Task<NoContent> Login(LoginRequest request, SignInManager<AppUser> signIn, ILoggerFactory loggers)
    {
        // isPersistent: the session survives closing the browser, until 7 days without activity.
        var result = await signIn.PasswordSignInAsync(NormalizeEmail(request.Email), request.Password, isPersistent: true, lockoutOnFailure: true);
        if (result.Succeeded)
        {
            return TypedResults.NoContent();
        }

        var (code, message) = result switch
        {
            { IsLockedOut: true } => (ErrorCodes.LockedOut, "Too many failed attempts; try again in 15 minutes or reset the password."),
            { IsNotAllowed: true } => (ErrorCodes.EmailNotConfirmed, "The email address has not been verified."),
            _ => (ErrorCodes.InvalidCredentials, "Wrong email or password.")
        };

        // Security event (ERR-AUTH-003, ERR-AUTH-005). Neither the password nor the email is logged.
        LogLoginRejected(loggers.CreateLogger("ContractGuard.Api.Auth"), code);
        throw new DomainException(code, message);
    }

    private static async Task<NoContent> Logout(SignInManager<AppUser> signIn)
    {
        await signIn.SignOutAsync();
        return TypedResults.NoContent();
    }

    // FR-AUTH-009, FR-AUTH-014: same answer whether or not the email has an account, and whether or
    // not this request was within the per-account limits.
    private static async Task<NoContent> ForgotPassword(
        EmailRequest request,
        UserManager<AppUser> users,
        AccountEmails emails,
        TimeProvider clock,
        CancellationToken cancellationToken)
    {
        var user = await users.FindByEmailAsync(NormalizeEmail(request.Email));
        if (user is not null && EmailThrottle.TryRecord(user.PasswordResetEmailsSentAt, clock.GetUtcNow()) is { } sent)
        {
            user.PasswordResetEmailsSentAt = sent;
            await users.UpdateAsync(user);
            await emails.SendPasswordResetAsync(user, cancellationToken);
        }

        return TypedResults.NoContent();
    }

    private static async Task<NoContent> ResetPassword(ResetPasswordRequest request, UserManager<AppUser> users)
    {
        var user = await users.FindByIdAsync(request.UserId.ToString());
        if (user is null)
        {
            throw LinkExpired();
        }

        var result = await users.ResetPasswordAsync(user, request.Token, request.NewPassword);
        if (result.Errors.Any(e => e.Code == nameof(IdentityErrorDescriber.InvalidToken)))
        {
            throw LinkExpired();
        }

        ThrowIfFailed(result);

        // ERR-AUTH-005: resetting the password is the way out of a lockout.
        await users.SetLockoutEndDateAsync(user, null);
        await users.ResetAccessFailedCountAsync(user);
        return TypedResults.NoContent();
    }

    private static async Task<Ok<MeResponse>> GetMe(
        ClaimsPrincipal principal,
        UserManager<AppUser> users,
        IApplicationDbContext db,
        CancellationToken cancellationToken)
    {
        var user = await users.GetUserAsync(principal)
            ?? throw new DomainException(ErrorCodes.Unauthenticated, "The account no longer exists.");

        var organizations = await db.Memberships
            .Where(m => m.UserAccountId == user.Id)
            .Join(
                db.Organizations,
                m => m.OrganizationId,
                o => o.OrganizationId,
                (m, o) => new WorkspaceResponse(o.OrganizationId, o.OrganizationName, m.OrgRole))
            .ToListAsync(cancellationToken);

        return TypedResults.Ok(new MeResponse(
            user.Id,
            user.DisplayName,
            user.Email!,
            user.PhoneNumber,
            user.UiLanguage,
            await users.HasPasswordAsync(user),
            [new WorkspaceResponse(null, null, null), .. organizations]));
    }

    // FR-AUTH-016. The language is what analyses and the assistant answer in (FR-RISK-013); the UI stays Vietnamese.
    private static async Task<NoContent> UpdateProfile(ProfileRequest request, ClaimsPrincipal principal, UserManager<AppUser> users)
    {
        var user = await users.GetUserAsync(principal)
            ?? throw new DomainException(ErrorCodes.Unauthenticated, "The account no longer exists.");
        if (!Enum.IsDefined(request.UiLanguage))
        {
            throw new DomainException(ErrorCodes.Validation, "Unknown language.");
        }

        user.DisplayName = ValidDisplayName(request.DisplayName);
        user.PhoneNumber = ValidPhoneNumber(request.PhoneNumber);
        user.UiLanguage = request.UiLanguage;
        ThrowIfFailed(await users.UpdateAsync(user));
        return TypedResults.NoContent();
    }

    // FR-AUTH-017. A new security stamp ends sessions elsewhere at their next check; this one is renewed.
    private static async Task<NoContent> ChangePassword(
        ChangePasswordRequest request,
        ClaimsPrincipal principal,
        UserManager<AppUser> users,
        SignInManager<AppUser> signIn)
    {
        var user = await users.GetUserAsync(principal)
            ?? throw new DomainException(ErrorCodes.Unauthenticated, "The account no longer exists.");
        var result = await users.HasPasswordAsync(user)
            ? await users.ChangePasswordAsync(user, request.CurrentPassword ?? string.Empty, request.NewPassword)
            : await users.AddPasswordAsync(user, request.NewPassword);
        if (result.Errors.Any(e => e.Code == nameof(IdentityErrorDescriber.PasswordMismatch)))
        {
            throw new DomainException(ErrorCodes.WrongCurrentPassword, "The current password is not correct.");
        }

        ThrowIfFailed(result);
        await signIn.RefreshSignInAsync(user);
        return TypedResults.NoContent();
    }

    private static string ValidDisplayName(string? name)
    {
        var trimmed = name?.Trim() ?? string.Empty;
        return trimmed.Length is 0 or > AppUser.DisplayNameMaxLength
            ? throw new DomainException(ErrorCodes.Validation, $"The name is required, at most {AppUser.DisplayNameMaxLength} characters.")
            : trimmed;
    }

    // Kept as digits with an optional leading +, so "0912 345 678" and "0912.345.678" are the same number.
    private static string? ValidPhoneNumber(string? phone)
    {
        if (string.IsNullOrWhiteSpace(phone))
        {
            return null;
        }

        var compact = PhoneSeparators().Replace(phone.Trim(), string.Empty);
        return PhoneDigits().IsMatch(compact)
            ? compact
            : throw new DomainException(ErrorCodes.Validation, "The phone number needs 8 to 15 digits, optionally starting with +.");
    }

    [GeneratedRegex(@"[\s\-.()]")]
    private static partial Regex PhoneSeparators();

    [GeneratedRegex(@"^\+?\d{8,15}$")]
    private static partial Regex PhoneDigits();

    private static string NormalizeEmail(string email)
    {
        email = email.Trim();
        return email.Length <= EmailMaxLength
            ? email
            : throw new DomainException(ErrorCodes.Validation, "The email address is too long.");
    }

    private static void ThrowIfFailed(IdentityResult result)
    {
        if (result.Succeeded)
        {
            return;
        }

        var codes = result.Errors.Select(e => e.Code).ToList();
        if (codes.Contains(nameof(IdentityErrorDescriber.DuplicateEmail)) || codes.Contains(nameof(IdentityErrorDescriber.DuplicateUserName)))
        {
            throw new DomainException(ErrorCodes.EmailTaken, "This email address is already registered.");
        }

        if (codes.Exists(c => c.StartsWith("Password", StringComparison.Ordinal)))
        {
            throw new DomainException(ErrorCodes.PasswordPolicy, "The password needs at least 8 characters with a letter and a digit.");
        }

        throw new DomainException(ErrorCodes.Validation, result.Errors.First().Description);
    }

    private static DomainException LinkExpired() =>
        new(ErrorCodes.LinkExpired, "The link is invalid or has expired.");

    [LoggerMessage(Level = LogLevel.Warning, Message = "Login rejected: {Code}")]
    private static partial void LogLoginRejected(ILogger logger, string code);
}
