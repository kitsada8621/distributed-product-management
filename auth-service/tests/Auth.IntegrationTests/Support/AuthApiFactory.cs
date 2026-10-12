using System.Security.Cryptography;
using Auth.Application.Emails;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Auth.IntegrationTests.Support;

public sealed class AuthApiFactory : WebApplicationFactory<Program>
{
    public const string AdminEmail = "admin@example.com";
    public const string AdminPassword = "Admin#12345";
    public const string SpaOrigin = "http://localhost:5173";
    public const string ResetBaseUrl = "http://localhost:5173/reset-password";

    private readonly string _directory = Path.Combine(Path.GetTempPath(), $"auth-api-{Guid.NewGuid():N}");
    private readonly InMemoryEmailSender _emailSender = new();

    public AuthApiFactory()
    {
        Directory.CreateDirectory(_directory);
        using var rsa = RSA.Create(2048);
        File.WriteAllText(KeyPath, rsa.ExportPkcs8PrivateKeyPem());
    }

    public string KeyPath => Path.Combine(_directory, "jwt-private.pem");

    public string DatabasePath => Path.Combine(_directory, "auth.db");

    public InMemoryEmailSender EmailSender => _emailSender;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:Auth"] = $"Data Source={DatabasePath};Pooling=False",
            ["Jwt:Issuer"] = "auth-service-tests",
            ["Jwt:PrivateKeyPath"] = KeyPath,
            ["Csrf:Secret"] = "integration-tests-csrf-secret-0123456789",
            ["Seed:AdminEmail"] = AdminEmail,
            ["Seed:AdminPassword"] = AdminPassword,
            ["Spa:Origin"] = SpaOrigin,
            ["ResetLink:BaseUrl"] = ResetBaseUrl,
            ["Smtp:Host"] = "smtp.invalid",
            ["Smtp:FromAddress"] = "no-reply@example.com"
        }));

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IEmailSender>();
            services.AddSingleton<IEmailSender>(_emailSender);
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (!disposing)
        {
            return;
        }

        SqliteConnection.ClearAllPools();
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }
}
