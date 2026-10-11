using Microsoft.AspNetCore.Diagnostics;

namespace Catalog.Api.Common;

public sealed class UnhandledExceptionHandler(ILogger<UnhandledExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken cancellationToken)
    {
        var response = exception is BadHttpRequestException
            ? ApiResponse.Fail(StatusCodes.Status400BadRequest, "BAD_REQUEST", "Malformed request")
            : ApiResponse.Fail(StatusCodes.Status500InternalServerError, "INTERNAL_ERROR", "An unexpected error occurred");

        logger.LogError(exception, "Request failed with status {Status}", response.Status);

        context.Response.StatusCode = response.Status;
        await context.Response.WriteAsJsonAsync(response, cancellationToken);
        return true;
    }
}
