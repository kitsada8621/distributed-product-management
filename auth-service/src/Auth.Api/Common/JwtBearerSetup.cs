using Auth.Infrastructure.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace Auth.Api.Common;

public static class JwtBearerSetup
{
    public static void AddJwtBearerAuth(this IServiceCollection services)
    {
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();

        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<JwtOptions>(ConfigureBearer);

        services.AddAuthorization();
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
            IssuerSigningKey = new RsaSecurityKey(RsaKeyLoader.Load(jwt.PrivateKeyPath)),
            ValidAlgorithms = [SecurityAlgorithms.RsaSha256],
            RoleClaimType = "role",
            NameClaimType = "sub"
        };
        options.Events = new JwtBearerEvents { OnChallenge = WriteUnauthorizedAsync };
    }

    private static Task WriteUnauthorizedAsync(JwtBearerChallengeContext context)
    {
        context.HandleResponse();
        var body = ApiResponse.Fail(context.HttpContext, StatusCodes.Status401Unauthorized, "UNAUTHORIZED", "Authentication required");
        context.Response.StatusCode = body.Status;
        return context.Response.WriteAsJsonAsync(body, context.HttpContext.RequestAborted);
    }
}
