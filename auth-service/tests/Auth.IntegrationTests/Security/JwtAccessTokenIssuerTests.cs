using System.Security.Cryptography;
using Auth.Domain.Users;
using Auth.Infrastructure.Security;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Auth.IntegrationTests.Security;

public sealed class JwtAccessTokenIssuerTests : IDisposable
{
    private const string Issuer = "auth-service-tests";

    private static readonly DateTimeOffset FixedNow = new(2026, 10, 10, 8, 30, 0, TimeSpan.Zero);

    private readonly string _keyPath = Path.Combine(Path.GetTempPath(), $"jwt-{Guid.NewGuid():N}.pem");
    private readonly RSA _rsa = RSA.Create(2048);
    private readonly User _user = User.Create("user@example.com", "Somchai", "hash", UserRole.Admin, FixedNow.UtcDateTime);

    public JwtAccessTokenIssuerTests()
    {
        File.WriteAllText(_keyPath, _rsa.ExportPkcs8PrivateKeyPem());
    }

    public void Dispose()
    {
        File.Delete(_keyPath);
        _rsa.Dispose();
    }

    [Fact]
    public async Task Issue_TokenValidatesWithPublicKeyAndCarriesTheClaims()
    {
        var token = CreateIssuer(new FixedTimeProvider(FixedNow)).Issue(_user);

        var result = await ValidateAsync(token, PublicKey(_rsa), validateLifetime: false);

        Assert.True(result.IsValid);
        Assert.Equal(_user.Id.ToString(), result.Claims["sub"]);
        Assert.Equal("admin", result.Claims["role"]);
        Assert.Equal("user@example.com", result.Claims["email"]);
        Assert.Equal(Issuer, result.Claims["iss"]);
    }

    [Fact]
    public void Issue_UsesRs256AndExpiresAfterFifteenMinutes()
    {
        var token = CreateIssuer(new FixedTimeProvider(FixedNow)).Issue(_user);

        var parsed = new JsonWebToken(token);

        Assert.Equal("RS256", parsed.Alg);
        Assert.Equal(FixedNow.UtcDateTime.AddMinutes(15), parsed.ValidTo);
    }

    [Fact]
    public async Task Issue_TokenIsValidRightNowWithRealClock()
    {
        var token = CreateIssuer(TimeProvider.System).Issue(_user);

        var result = await ValidateAsync(token, PublicKey(_rsa), validateLifetime: true);

        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task Issue_ValidationWithADifferentPublicKeyFails()
    {
        var token = CreateIssuer(new FixedTimeProvider(FixedNow)).Issue(_user);
        using var other = RSA.Create(2048);

        var result = await ValidateAsync(token, PublicKey(other), validateLifetime: false);

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Constructor_MissingKeyFile_ThrowsWithThePathInTheMessage()
    {
        var missing = Path.Combine(Path.GetTempPath(), "does-not-exist.pem");
        var options = new JwtOptions { Issuer = Issuer, PrivateKeyPath = missing };

        var exception = Assert.Throws<FileNotFoundException>(() => new JwtAccessTokenIssuer(options, TimeProvider.System));

        Assert.Contains(missing, exception.Message);
    }

    private JwtAccessTokenIssuer CreateIssuer(TimeProvider timeProvider)
    {
        var options = new JwtOptions { Issuer = Issuer, PrivateKeyPath = _keyPath };
        return new JwtAccessTokenIssuer(options, timeProvider);
    }

    private static RsaSecurityKey PublicKey(RSA privateKey)
    {
        var publicOnly = RSA.Create();
        publicOnly.ImportSubjectPublicKeyInfo(privateKey.ExportSubjectPublicKeyInfo(), out _);
        return new RsaSecurityKey(publicOnly);
    }

    private static Task<TokenValidationResult> ValidateAsync(string token, SecurityKey key, bool validateLifetime)
    {
        var parameters = new TokenValidationParameters
        {
            ValidIssuer = Issuer,
            IssuerSigningKey = key,
            ValidAlgorithms = [SecurityAlgorithms.RsaSha256],
            ValidateAudience = false,
            ValidateLifetime = validateLifetime
        };
        return new JsonWebTokenHandler().ValidateTokenAsync(token, parameters);
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
