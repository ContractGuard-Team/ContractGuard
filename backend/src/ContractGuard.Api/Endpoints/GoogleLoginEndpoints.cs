using System.Security.Claims;
using ContractGuard.Api.Http;
using ContractGuard.Infrastructure.Email;
using ContractGuard.Infrastructure.Identity;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace ContractGuard.Api.Endpoints;

// F-AUTH-002. The browser navigates here (no fetch): /google sends it to Google, Google returns to
// CallbackPath where the handler keeps the result in the external cookie, then /google/complete
// signs in and sends the browser back to the SPA.
internal static partial class GoogleLoginEndpoints
{
    // Under /api/ so nginx, Vite and later CloudFront forward it to the API like any other call.
    public const string CallbackPath = "/api/v1/auth/google/callback";
    private const string CompletePath = "/api/v1/auth/google/complete";
    private const string DefaultNext = "/contracts";

    public static RouteGroupBuilder MapGoogleLogin(this RouteGroupBuilder auth)
    {
        auth.MapGet("/google", Start).RequireRateLimiting(RateLimits.Login);
        auth.MapGet("/google/complete", Complete);
        return auth;
    }

    // The SPA login page with a reason it shows as a notice.
    public static string LoginPage(EmailOptions options, string reason) =>
        $"{options.FrontendBaseUrl.TrimEnd('/')}/login?google={reason}";

    private static async Task<IResult> Start(
        string? next,
        IAuthenticationSchemeProvider schemes,
        SignInManager<AppUser> signIn,
        IOptions<EmailOptions> options)
    {
        // No client id and secret configured: the button stays, the page says Google is not enabled.
        if (await schemes.GetSchemeAsync(GoogleAccounts.Provider) is null)
        {
            return TypedResults.Redirect(LoginPage(options.Value, "unavailable"));
        }

        var redirect = $"{CompletePath}?next={Uri.EscapeDataString(SafeNext(next))}";
        var properties = signIn.ConfigureExternalAuthenticationProperties(GoogleAccounts.Provider, redirect);
        return TypedResults.Challenge(properties, [GoogleAccounts.Provider]);
    }

    private static async Task<RedirectHttpResult> Complete(
        string? next,
        HttpContext http,
        SignInManager<AppUser> signIn,
        GoogleAccounts accounts,
        IOptions<EmailOptions> options,
        ILoggerFactory loggers)
    {
        var logger = loggers.CreateLogger("ContractGuard.Api.Auth");
        var info = await signIn.GetExternalLoginInfoAsync();
        await http.SignOutAsync(IdentityConstants.ExternalScheme);
        if (info is null || info.LoginProvider != GoogleAccounts.Provider)
        {
            LogGoogleRejected(logger, "no Google result");
            return TypedResults.Redirect(LoginPage(options.Value, "failed"));
        }

        // Linking by email is only safe for an address Google itself has verified.
        var email = info.Principal.FindFirstValue(ClaimTypes.Email)?.Trim();
        var verified = string.Equals(info.Principal.FindFirstValue("email_verified"), "true", StringComparison.OrdinalIgnoreCase);
        if (string.IsNullOrEmpty(email) || !verified)
        {
            LogGoogleRejected(logger, "email not verified by Google");
            return TypedResults.Redirect(LoginPage(options.Value, "failed"));
        }

        AppUser user;
        try
        {
            user = await accounts.FindOrCreateAsync(info.ProviderKey, email, info.Principal.FindFirstValue(ClaimTypes.Name));
        }
        catch (InvalidOperationException exception)
        {
            LogGoogleRejected(logger, exception.Message);
            return TypedResults.Redirect(LoginPage(options.Value, "failed"));
        }

        // SignInAsync rather than ExternalLoginSignInAsync: the lockout of SEC-AUTH-006 is for
        // password sign-in only, and Google has just proven who this is (P6-01).
        await signIn.SignInAsync(user, isPersistent: true, authenticationMethod: GoogleAccounts.Provider);
        return TypedResults.Redirect($"{options.Value.FrontendBaseUrl.TrimEnd('/')}{SafeNext(next)}");
    }

    // Same rule as safeNext in the SPA: a path on this site, never another host.
    internal static string SafeNext(string? next) =>
        next is ['/', ..] && !next.StartsWith("//", StringComparison.Ordinal) && !next.StartsWith("/\\", StringComparison.Ordinal)
            ? next
            : DefaultNext;

    [LoggerMessage(Level = LogLevel.Warning, Message = "Google login rejected: {Reason}")]
    private static partial void LogGoogleRejected(ILogger logger, string reason);
}
