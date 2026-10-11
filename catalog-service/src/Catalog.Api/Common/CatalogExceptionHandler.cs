using Catalog.Application.Common;
using Microsoft.AspNetCore.Diagnostics;

namespace Catalog.Api.Common;

public sealed class CatalogExceptionHandler(ILogger<CatalogExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken cancellationToken)
    {
        if (exception is not CatalogException catalogException)
        {
            return false;
        }

        var status = StatusFor(catalogException.Code);
        if (status is null)
        {
            return false;
        }

        logger.LogInformation("Request rejected with {Code}", catalogException.Code);

        var errors = catalogException.Errors?.Select(e => new FieldErrorItem(e.Field, e.Code, e.Message)).ToList();
        var body = ApiResponse.Fail(status.Value, catalogException.Code, catalogException.Message, errors);
        context.Response.StatusCode = body.Status;
        await context.Response.WriteAsJsonAsync(body, cancellationToken);
        return true;
    }

    // API-SPEC §8.1. An unknown code falls through to the 500 handler.
    private static int? StatusFor(string code)
    {
        return code switch
        {
            ErrorCodes.ValidationError => StatusCodes.Status422UnprocessableEntity,
            ErrorCodes.StockNegative => StatusCodes.Status422UnprocessableEntity,
            ErrorCodes.Unauthorized => StatusCodes.Status401Unauthorized,
            ErrorCodes.Forbidden => StatusCodes.Status403Forbidden,
            ErrorCodes.NotFound => StatusCodes.Status404NotFound,
            ErrorCodes.CategoryNameExists => StatusCodes.Status409Conflict,
            _ => null
        };
    }
}
