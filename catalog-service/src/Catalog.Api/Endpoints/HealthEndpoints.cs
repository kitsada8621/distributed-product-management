using Catalog.Api.Common;

namespace Catalog.Api.Endpoints;

public static class HealthEndpoints
{
    public static void MapHealthEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/catalog/health", () =>
            ApiResponse.Ok(StatusCodes.Status200OK, "OK", new { status = "ok" }).ToResult());
    }
}
