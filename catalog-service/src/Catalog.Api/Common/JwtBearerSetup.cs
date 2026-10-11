using Catalog.Infrastructure.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace Catalog.Api.Common;

public static class JwtBearerSetup
{
    public const string AdminPolicy = "Admin";

    public static void AddJwtBearerAuth(this IServiceCollection services)
    {
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();

        // Configured lazily from JwtOptions so the key file is read when first needed.
        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<JwtOptions>(ConfigureBearer);

        services.AddAuthorization(options =>
            options.AddPolicy(AdminPolicy, policy => policy.RequireRole("admin")));
    }

    private static void ConfigureBearer(JwtBearerOptions options, JwtOptions jwt)
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwt.Issuer,
            ValidateAudience = false,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new RsaSecurityKey(RsaPublicKeyLoader.Load(jwt.PublicKeyPath)),
            ValidAlgorithms = [SecurityAlgorithms.RsaSha256],
            RoleClaimType = "role",
            NameClaimType = "sub"
        };
        options.Events = new JwtBearerEvents
        {
            OnChallenge = WriteUnauthorizedAsync,
            OnForbidden = WriteForbiddenAsync
        };
    }

    private static Task WriteUnauthorizedAsync(JwtBearerChallengeContext context)
    {
        context.HandleResponse();
        var body = ApiResponse.Fail(StatusCodes.Status401Unauthorized, "UNAUTHORIZED", "Authentication required");
        context.Response.StatusCode = body.Status;
        return context.Response.WriteAsJsonAsync(body, context.HttpContext.RequestAborted);
    }

    private static Task WriteForbiddenAsync(ForbiddenContext context)
    {
        var body = ApiResponse.Fail(StatusCodes.Status403Forbidden, "FORBIDDEN", "You do not have permission");
        context.Response.StatusCode = body.Status;
        return context.Response.WriteAsJsonAsync(body, context.HttpContext.RequestAborted);
    }
}
