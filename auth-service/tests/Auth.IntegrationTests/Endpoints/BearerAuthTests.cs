using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text.Json;
using Auth.Domain.Users;
using Auth.Infrastructure.Security;
using Auth.IntegrationTests.Support;

namespace Auth.IntegrationTests.Endpoints;

public sealed class BearerAuthTests(AuthApiFactory factory) : IClassFixture<AuthApiFactory>, IDisposable
{
    private const string Issuer = "auth-service-tests";

    private readonly User _user = User.Create("user@example.com", "Somchai", "hash", UserRole.User, DateTime.UtcNow);
    private readonly List<string> _tempFiles = [];

    public void Dispose()
    {
        foreach (var file in _tempFiles)
        {
            File.Delete(file);
        }
    }

    [Fact]
    public async Task NoAuthorizationHeader_Returns401Envelope()
    {
        var client = TestEndpointsStartupFilter.AddTo(factory).CreateClient();

        var response = await client.GetAsync("/test/secure");

        await AssertUnauthorizedAsync(response);
    }

    [Fact]
    public async Task GarbageToken_Returns401Envelope()
    {
        var response = await GetSecureAsync("not.a.jwt");

        await AssertUnauthorizedAsync(response);
    }

    [Fact]
    public async Task ExpiredToken_Returns401Envelope()
    {
        var token = CreateIssuer(Issuer, factory.KeyPath, new FixedTimeProvider(DateTimeOffset.UtcNow.AddHours(-1))).Issue(_user);

        var response = await GetSecureAsync(token);

        await AssertUnauthorizedAsync(response);
    }

    [Fact]
    public async Task TokenSignedByAnotherKey_Returns401Envelope()
    {
        var token = CreateIssuer(Issuer, CreateOtherKeyFile(), TimeProvider.System).Issue(_user);

        var response = await GetSecureAsync(token);

        await AssertUnauthorizedAsync(response);
    }

    [Fact]
    public async Task TokenWithWrongIssuer_Returns401Envelope()
    {
        var token = CreateIssuer("someone-else", factory.KeyPath, TimeProvider.System).Issue(_user);

        var response = await GetSecureAsync(token);

        await AssertUnauthorizedAsync(response);
    }

    [Fact]
    public async Task ValidToken_ReachesTheEndpointWithTheSubClaim()
    {
        var token = CreateIssuer(Issuer, factory.KeyPath, TimeProvider.System).Issue(_user);

        var response = await GetSecureAsync(token);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal($"\"{_user.Id}\"", await response.Content.ReadAsStringAsync());
    }

    private async Task<HttpResponseMessage> GetSecureAsync(string token)
    {
        var client = TestEndpointsStartupFilter.AddTo(factory).CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return await client.GetAsync("/test/secure");
    }

    private static async Task AssertUnauthorizedAsync(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = json.RootElement;
        Assert.Equal("UNAUTHORIZED", root.GetProperty("code").GetString());
        Assert.Equal(401, root.GetProperty("status").GetInt32());
        Assert.False(root.GetProperty("success").GetBoolean());
        Assert.False(string.IsNullOrEmpty(root.GetProperty("correlationId").GetString()));
        Assert.True(response.Headers.Contains("X-Correlation-Id"));
    }

    private static JwtAccessTokenIssuer CreateIssuer(string issuer, string keyPath, TimeProvider timeProvider)
    {
        return new JwtAccessTokenIssuer(new JwtOptions { Issuer = issuer, PrivateKeyPath = keyPath }, timeProvider);
    }

    private string CreateOtherKeyFile()
    {
        var path = Path.Combine(Path.GetTempPath(), $"other-key-{Guid.NewGuid():N}.pem");
        using var rsa = RSA.Create(2048);
        File.WriteAllText(path, rsa.ExportPkcs8PrivateKeyPem());
        _tempFiles.Add(path);
        return path;
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
