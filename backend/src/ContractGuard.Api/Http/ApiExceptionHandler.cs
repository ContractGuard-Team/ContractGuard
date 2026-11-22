using ContractGuard.Domain.Common;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace ContractGuard.Api.Http;

// Turns a business rule violation into Problem Details (RFC 9457) with the ERR-* code (SD-09).
// Any other exception is not handled here and becomes a 500 without internal details.
internal sealed class ApiExceptionHandler(IProblemDetailsService problemDetails) : IExceptionHandler
{
    private static readonly Dictionary<string, int> StatusByCode = new()
    {
        [ErrorCodes.EmailTaken] = StatusCodes.Status409Conflict,
        [ErrorCodes.InvalidCredentials] = StatusCodes.Status401Unauthorized,
        [ErrorCodes.Unauthenticated] = StatusCodes.Status401Unauthorized,
        [ErrorCodes.LockedOut] = StatusCodes.Status403Forbidden,
        [ErrorCodes.EmailNotConfirmed] = StatusCodes.Status403Forbidden,
        [ErrorCodes.Forbidden] = StatusCodes.Status403Forbidden,
        [ErrorCodes.NotFound] = StatusCodes.Status404NotFound,
        [ErrorCodes.InvalidStatusTransition] = StatusCodes.Status409Conflict,
        [ErrorCodes.ContractBusy] = StatusCodes.Status409Conflict,
        [ErrorCodes.SourceFileMissing] = StatusCodes.Status409Conflict,
        [ErrorCodes.EmailSendFailed] = StatusCodes.Status503ServiceUnavailable,
        [ErrorCodes.AnalysisFailed] = StatusCodes.Status503ServiceUnavailable,
        [ErrorCodes.ExportFailed] = StatusCodes.Status500InternalServerError,
        [ErrorCodes.ChatFailed] = StatusCodes.Status503ServiceUnavailable,
    };

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var problem = exception switch
        {
            DomainException domain => new ProblemDetails
            {
                Status = StatusByCode.GetValueOrDefault(domain.ErrorCode, StatusCodes.Status400BadRequest),
                Detail = domain.Message,
                Extensions = { ["code"] = domain.ErrorCode }
            },

            // Malformed JSON. Thrown only in Development; other environments answer 400 directly.
            BadHttpRequestException badRequest => new ProblemDetails { Status = badRequest.StatusCode },
            _ => null
        };
        if (problem is null)
        {
            return false;
        }

        httpContext.Response.StatusCode = problem.Status!.Value;
        return await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = problem
        });
    }
}
