using Auth.Api.Common;

namespace Auth.Api.Endpoints;

public static class HealthEndpoints
{
    public static void MapHealthEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/auth/health", (HttpContext context) =>
            ApiResponse.Ok(context, StatusCodes.Status200OK, "OK", new { status = "ok" }).ToResult());
    }
}
