using ContractGuard.Infrastructure.Identity;
using ContractGuard.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;

namespace ContractGuard.Api.Http;

// ASP.NET Core Identity with a session cookie (SD-04).
internal static class AuthSetup
{
    public const string CsrfHeaderName = "X-CSRF-TOKEN";

    public static IServiceCollection AddApiAuth(this IServiceCollection services, IWebHostEnvironment environment)
    {
        // Development and tests run on plain http://localhost, where a Secure cookie would not be sent.
        var securePolicy = environment.IsProduction() ? CookieSecurePolicy.Always : CookieSecurePolicy.SameAsRequest;

        services.AddAuthentication(IdentityConstants.ApplicationScheme).AddIdentityCookies();
        services.AddAuthorization();

        services.AddIdentityCore<AppUser>(options =>
            {
                options.User.RequireUniqueEmail = true; // FR-AUTH-002

                // The email is the user name, so every valid email must pass the user name check too.
                options.User.AllowedUserNameCharacters = string.Empty;

                options.SignIn.RequireConfirmedEmail = true; // FR-AUTH-008

                // FR-AUTH-005: 8 characters with a letter and a digit. The letter rule is
                // PasswordHasLetterValidator, because RequireLowercase would refuse "ABCD1234".
                options.Password.RequiredLength = 8;
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = false;
                options.Password.RequireUppercase = false;
                options.Password.RequireNonAlphanumeric = false;

                // SEC-AUTH-006
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
            })
            .AddEntityFrameworkStores<AppDbContext>()
            .AddSignInManager()
            .AddDefaultTokenProviders()
            .AddPasswordValidator<PasswordHasLetterValidator>();

        // Verification and reset links last 10 minutes (OQ-AUTH-001).
        services.Configure<DataProtectionTokenProviderOptions>(options => options.TokenLifespan = TimeSpan.FromMinutes(10));

        services.ConfigureApplicationCookie(options =>
        {
            options.Cookie.Name = "cg_session";
            options.Cookie.HttpOnly = true;
            options.Cookie.SameSite = SameSiteMode.Lax;
            options.Cookie.SecurePolicy = securePolicy;

            // SEC-AUTH-003: the session ends after 7 days without activity.
            options.ExpireTimeSpan = TimeSpan.FromDays(7);
            options.SlidingExpiration = true;

            // This is a JSON API: answer with a status code instead of redirecting to a login page.
            options.Events = new CookieAuthenticationEvents
            {
                OnRedirectToLogin = context => WriteStatus(context.Response, StatusCodes.Status401Unauthorized),
                OnRedirectToAccessDenied = context => WriteStatus(context.Response, StatusCodes.Status403Forbidden)
            };
        });

        services.AddAntiforgery(options =>
        {
            options.HeaderName = CsrfHeaderName;
            options.Cookie.Name = "cg_csrf";
            options.Cookie.SameSite = SameSiteMode.Lax;
            options.Cookie.SecurePolicy = securePolicy;
        });

        return services;
    }

    private static Task WriteStatus(HttpResponse response, int status)
    {
        response.StatusCode = status;
        return Task.CompletedTask;
    }
}
