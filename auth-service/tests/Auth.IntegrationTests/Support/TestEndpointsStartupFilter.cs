using System.Security.Claims;
using Auth.Application.Common;
using Auth.Application.Users;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Auth.IntegrationTests.Support;

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
        endpoints.MapGet("/test/secure", (ClaimsPrincipal user) => Results.Ok(user.FindFirst("sub")?.Value))
            .RequireAuthorization();
        endpoints.MapGet("/test/throw/{code}", IResult (string code) => throw CreateException(code));
    }

    private static AuthException CreateException(string code)
    {
        if (code == ErrorCodes.ValidationError)
        {
            var errors = new[] { new FieldError("email", ErrorCodes.InvalidFormat, "Invalid email format") };
            return new AuthException(code, "Invalid input", errors);
        }

        if (code == ErrorCodes.AccountLocked)
        {
            return new AuthException(code, "Account is locked", data: new AccountLockedData(DateTime.UtcNow.AddMinutes(15)));
        }

        return new AuthException(code, "Test message");
    }
}
