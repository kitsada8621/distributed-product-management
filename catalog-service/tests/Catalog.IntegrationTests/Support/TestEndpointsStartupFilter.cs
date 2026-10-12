using Catalog.Application.Common;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Catalog.IntegrationTests.Support;

// Adds endpoints that only exist in tests: protected and anonymous ones, and ones that throw.
public sealed class TestEndpointsStartupFilter : IStartupFilter
{
    public static WebApplicationFactory<Program> AddTo(WebApplicationFactory<Program> factory)
    {
        return factory.WithWebHostBuilder(builder => builder.ConfigureServices(
            services => services.AddSingleton<IStartupFilter, TestEndpointsStartupFilter>()));
    }

    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
    {
        return app =>
        {
            next(app);
            app.UseRouting();
            app.UseAuthentication();
            app.UseAuthorization();
            app.UseEndpoints(MapTestEndpoints);
        };
    }

    private static void MapTestEndpoints(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/test/throw", IResult () => throw new InvalidOperationException("secret failure detail"));
        endpoints.MapPost("/test/body", (TestBody body) => Results.Ok(body.Name));
        endpoints.MapGet("/test/admin", () => Results.Ok("admin-area")).RequireAuthorization("Admin");
        endpoints.MapGet("/test/anonymous", (HttpContext context) => Results.Ok(CallerRole(context)))
            .AllowAnonymous();
        endpoints.MapGet("/test/throw-{code}", IResult (string code) => throw CreateException(code));
    }

    private static string CallerRole(HttpContext context)
    {
        if (context.User.IsInRole("admin"))
        {
            return "admin";
        }

        return context.User.IsInRole("user") ? "user" : "guest";
    }

    private static CatalogException CreateException(string code)
    {
        if (code == ErrorCodes.ValidationError)
        {
            var errors = new[] { new FieldError("name", ErrorCodes.Required, "Name is required") };
            return new CatalogException(code, "Invalid input", errors);
        }

        return new CatalogException(code, "Test message");
    }

    private sealed record TestBody(string Name);
}
