using ContractGuard.Domain.Common;
using Microsoft.AspNetCore.Antiforgery;

namespace ContractGuard.Api.Http;

// SD-04: every write request must carry the antiforgery token in a header. UseAntiforgery() only
// rejects form posts by itself, not JSON, so the check is done here. GET, HEAD, OPTIONS always pass.
internal sealed class CsrfEndpointFilter(IAntiforgery antiforgery) : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        if (!await antiforgery.IsRequestValidAsync(context.HttpContext))
        {
            throw new DomainException(ErrorCodes.CsrfInvalid, "Missing or invalid CSRF token.");
        }

        return await next(context);
    }
}
