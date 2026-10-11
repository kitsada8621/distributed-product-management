using Auth.Application.Common;
using Microsoft.AspNetCore.Diagnostics;

namespace Auth.Api.Common;

public sealed class AuthExceptionHandler(ILogger<AuthExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken cancellationToken)
    {
        if (exception is not AuthException authException)
        {
            return false;
        }

        var status = StatusFor(authException.Code);
        if (status is null)
        {
            return false;
        }

        logger.LogInformation("Request rejected with {Code}", authException.Code);

        var errors = authException.Errors?.Select(e => new FieldErrorItem(e.Field, e.Code, e.Message)).ToList();
        var body = ApiResponse.Fail(context, status.Value, authException.Code, authException.Message, errors, authException.Data);
        context.Response.StatusCode = body.Status;
        await context.Response.WriteAsJsonAsync(body, cancellationToken);
        return true;
    }

    private static int? StatusFor(string code)
    {
        return code switch
        {
            ErrorCodes.ValidationError => StatusCodes.Status422UnprocessableEntity,
            ErrorCodes.InvalidCredentials => StatusCodes.Status401Unauthorized,
            ErrorCodes.RefreshInvalid => StatusCodes.Status401Unauthorized,
            ErrorCodes.RefreshReused => StatusCodes.Status401Unauthorized,
            ErrorCodes.Unauthorized => StatusCodes.Status401Unauthorized,
            ErrorCodes.CsrfInvalid => StatusCodes.Status403Forbidden,
            ErrorCodes.NotFound => StatusCodes.Status404NotFound,
            ErrorCodes.EmailExists => StatusCodes.Status409Conflict,
            ErrorCodes.ResetTokenInvalid => StatusCodes.Status400BadRequest,
            ErrorCodes.AccountLocked => StatusCodes.Status423Locked,
            _ => null
        };
    }
}
