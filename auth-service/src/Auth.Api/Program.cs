using Auth.Api.Common;
using Auth.Api.Endpoints;
using Auth.Api.Workers;
using Auth.Application.Common;
using Auth.Application.Emails;
using Auth.Application.Security;
using Auth.Application.Tokens;
using Auth.Application.Users;
using Auth.Infrastructure.Email;
using Auth.Infrastructure.Persistence;
using Auth.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
var services = builder.Services;

services.AddOpenApi();
services.AddExceptionHandler<AuthExceptionHandler>();
services.AddExceptionHandler<UnhandledExceptionHandler>();
services.Configure<RouteHandlerOptions>(options => options.ThrowOnBadRequest = true);
services.AddJwtBearerAuth();

BindSection<JwtOptions>("Jwt");
BindSection<CsrfOptions>("Csrf");
BindSection<SmtpOptions>("Smtp");
BindSection<SeedOptions>("Seed");
BindSection<ResetLinkOptions>("ResetLink");
BindSection<SpaOptions>("Spa");
BindSection<AuthCookieOptions>("AuthCookie");
BindSection<EmailWorkerOptions>("EmailWorker");

services.AddSingleton(TimeProvider.System);
services.AddDbContext<AuthDbContext>((serviceProvider, options) => options
    .UseSqlite(serviceProvider.GetRequiredService<IConfiguration>().GetConnectionString("Auth"))
    .AddInterceptors(new SqlitePragmaInterceptor()));
services.AddScoped<IUnitOfWork>(serviceProvider => serviceProvider.GetRequiredService<AuthDbContext>());

services.AddScoped<IUserRepository, UserRepository>();
services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
services.AddScoped<IResetTokenRepository, ResetTokenRepository>();
services.AddScoped<IEmailOutboxRepository, EmailOutboxRepository>();

services.AddSingleton<IPasswordHasher, Argon2PasswordHasher>();
services.AddSingleton<IAccessTokenIssuer, JwtAccessTokenIssuer>();
services.AddSingleton<ISecureTokenService, SecureTokenService>();
services.AddSingleton<IEmailSender, SmtpEmailSender>();

services.AddScoped<AdminSeeder>();
services.AddScoped<RegisterUser>();
services.AddScoped<LoginUser>();
services.AddScoped<RefreshSession>();
services.AddScoped<LogoutSession>();
services.AddScoped<ForgotPassword>();
services.AddScoped<ResetPassword>();
services.AddScoped<GetCurrentUser>();
services.AddScoped<SendPendingEmails>();
services.AddHostedService<EmailOutboxWorker>();

var app = builder.Build();

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseExceptionHandler(_ => { });
app.UseStatusCodePages(async statusContext =>
{
    var httpContext = statusContext.HttpContext;
    if (httpContext.Response.StatusCode == StatusCodes.Status404NotFound)
    {
        var response = ApiResponse.Fail(httpContext, StatusCodes.Status404NotFound, "NOT_FOUND", "Resource not found");
        await httpContext.Response.WriteAsJsonAsync(response);
    }
});
app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapHealthEndpoints();
app.MapAuthEndpoints();

using (var scope = app.Services.CreateScope())
{
    var database = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
    await database.Database.MigrateAsync();
    await scope.ServiceProvider.GetRequiredService<AdminSeeder>().SeedAsync(CancellationToken.None);
}

app.Run();

void BindSection<T>(string section) where T : class, new()
{
    services.AddSingleton(serviceProvider =>
        serviceProvider.GetRequiredService<IConfiguration>().GetSection(section).Get<T>() ?? new T());
}

public partial class Program;
