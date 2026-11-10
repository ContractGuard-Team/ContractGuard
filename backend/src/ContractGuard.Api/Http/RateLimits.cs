using System.Threading.RateLimiting;
using ContractGuard.Domain.Common;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ContractGuard.Api.Http;

// SEC-AUTH-007: per IP, 30 login and 10 password reset requests a minute; the rest get ERR-AUTH-006.
// The numbers come from configuration (RateLimits:LoginPerMinute, RateLimits:PasswordResetPerMinute)
// so integration tests, which all come from one address, can raise them.
// Phase 7 (SD-03): behind the ALB the client address is in X-Forwarded-For; until
// ForwardedHeaders also trusts that header, every request shares the ALB's address.
internal static class RateLimits
{
    public const string Login = "login";
    public const string PasswordReset = "password-reset";

    public static IServiceCollection AddApiRateLimits(this IServiceCollection services, IConfiguration configuration)
    {
        var loginLimit = configuration.GetValue("RateLimits:LoginPerMinute", 30);
        var resetLimit = configuration.GetValue("RateLimits:PasswordResetPerMinute", 10);

        services.AddRateLimiter(options =>
        {
            options.AddPolicy(Login, context => PerIp(context, loginLimit));
            options.AddPolicy(PasswordReset, context => PerIp(context, resetLimit));
            options.OnRejected = async (rejected, cancellationToken) =>
            {
                var http = rejected.HttpContext;
                http.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                http.Response.Headers.RetryAfter = "60";
                await http.RequestServices.GetRequiredService<IProblemDetailsService>().WriteAsync(new ProblemDetailsContext
                {
                    HttpContext = http,
                    ProblemDetails = new ProblemDetails
                    {
                        Status = StatusCodes.Status429TooManyRequests,
                        Title = "Too many requests",
                        Extensions = { ["code"] = ErrorCodes.TooManyRequests }
                    }
                });
            };
        });
        return services;
    }

    private static RateLimitPartition<string> PerIp(HttpContext context, int permitsPerMinute) =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions { PermitLimit = permitsPerMinute, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 });
}
