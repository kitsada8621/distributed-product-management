using System.Security.Cryptography;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Catalog.IntegrationTests.Support;

public sealed class CatalogApiFactory : WebApplicationFactory<Program>
{
    public const string Issuer = "auth-service-tests";

    private readonly string _directory = Path.Combine(Path.GetTempPath(), $"catalog-api-{Guid.NewGuid():N}");
    private readonly RSA _signingKey = RSA.Create(2048);

    public CatalogApiFactory()
    {
        Directory.CreateDirectory(_directory);
        File.WriteAllText(PublicKeyPath, _signingKey.ExportSubjectPublicKeyInfoPem());
    }

    public string PublicKeyPath => Path.Combine(_directory, "jwt-public.pem");

    public string DatabasePath => Path.Combine(_directory, "catalog.db");

    public string ConnectionString => $"Data Source={DatabasePath};Pooling=False";

    // The optional parameters build the bad tokens the auth tests need.
    public string CreateToken(
        string role,
        string? userId = null,
        string issuer = Issuer,
        bool expired = false,
        SigningCredentials? credentials = null)
    {
        var now = DateTime.UtcNow;
        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = issuer,
            NotBefore = expired ? now.AddHours(-2) : now.AddMinutes(-1),
            Expires = expired ? now.AddHours(-1) : now.AddMinutes(15),
            SigningCredentials = credentials ?? new SigningCredentials(new RsaSecurityKey(_signingKey), SecurityAlgorithms.RsaSha256),
            Claims = new Dictionary<string, object>
            {
                ["sub"] = userId ?? Guid.NewGuid().ToString(),
                ["role"] = role,
                ["email"] = "user@example.com"
            }
        };
        return new JsonWebTokenHandler().CreateToken(descriptor);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // UseSetting is visible to Program.cs while it registers services; AddInMemoryCollection is not.
        builder.UseSetting("Kafka:Enabled", "false");
        builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:Catalog"] = ConnectionString,
            ["Jwt:Issuer"] = Issuer,
            ["Jwt:PublicKeyPath"] = PublicKeyPath,
            ["Kafka:RetryDelayMs"] = "0"
        }));
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (!disposing)
        {
            return;
        }

        _signingKey.Dispose();
        SqliteConnection.ClearAllPools();
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }
}
