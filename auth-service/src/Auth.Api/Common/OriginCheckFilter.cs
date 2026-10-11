using Auth.Application.Common;

namespace Auth.Api.Common;

public sealed class OriginCheckFilter(SpaOptions spa) : IEndpointFilter
{
    public ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var origin = context.HttpContext.Request.Headers.Origin.ToString();
        if (origin != spa.Origin)
        {
            throw new AuthException(ErrorCodes.CsrfInvalid, "Invalid CSRF token");
        }

        return next(context);
    }
}
