using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Catalog.IntegrationTests.Support;
using Microsoft.IdentityModel.Tokens;

namespace Catalog.IntegrationTests.Endpoints;

public sealed class BearerAuthTests(CatalogApiFactory factory) : IClassFixture<CatalogApiFactory>
{
    [Fact]
    public async Task AdminEndpoint_WithoutToken_Returns401Envelope()
    {
        var response = await GetAsync("/test/admin", token: null);

        await AssertErrorAsync(response, HttpStatusCode.Unauthorized, "UNAUTHORIZED");
    }

    [Fact]
    public async Task AdminEndpoint_ExpiredToken_Returns401()
    {
        var response = await GetAsync("/test/admin", factory.CreateToken("admin", expired: true));

        await AssertErrorAsync(response, HttpStatusCode.Unauthorized, "UNAUTHORIZED");
    }

    [Fact]
    public async Task AdminEndpoint_WrongIssuer_Returns401()
    {
        var response = await GetAsync("/test/admin", factory.CreateToken("admin", issuer: "someone-else"));

        await AssertErrorAsync(response, HttpStatusCode.Unauthorized, "UNAUTHORIZED");
    }

    [Fact]
    public async Task AdminEndpoint_TokenSignedByAnotherKey_Returns401()
    {
        using var otherKey = RSA.Create(2048);
        var credentials = new SigningCredentials(new RsaSecurityKey(otherKey), SecurityAlgorithms.RsaSha256);

        var response = await GetAsync("/test/admin", factory.CreateToken("admin", credentials: credentials));

        await AssertErrorAsync(response, HttpStatusCode.Unauthorized, "UNAUTHORIZED");
    }

    [Fact]
    public async Task AdminEndpoint_TokenUsingHs256_Returns401()
    {
        var secret = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("a-shared-secret-that-is-at-least-32-bytes-long"));
        var credentials = new SigningCredentials(secret, SecurityAlgorithms.HmacSha256);

        var response = await GetAsync("/test/admin", factory.CreateToken("admin", credentials: credentials));

        await AssertErrorAsync(response, HttpStatusCode.Unauthorized, "UNAUTHORIZED");
    }

    [Fact]
    public async Task AdminEndpoint_RoleUser_Returns403Envelope()
    {
        var response = await GetAsync("/test/admin", factory.CreateToken("user"));

        await AssertErrorAsync(response, HttpStatusCode.Forbidden, "FORBIDDEN");
    }

    [Fact]
    public async Task AdminEndpoint_RoleAdmin_Returns200()
    {
        var response = await GetAsync("/test/admin", factory.CreateToken("admin"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task AnonymousEndpoint_WithoutToken_IsGuest()
    {
        var response = await GetAsync("/test/anonymous", token: null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("\"guest\"", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task AnonymousEndpoint_InvalidToken_IsGuestNotUnauthorized()
    {
        var response = await GetAsync("/test/anonymous", "not.a.jwt");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("\"guest\"", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task AnonymousEndpoint_ValidAdminToken_IsAdmin()
    {
        var response = await GetAsync("/test/anonymous", factory.CreateToken("admin"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("\"admin\"", await response.Content.ReadAsStringAsync());
    }

    private async Task<HttpResponseMessage> GetAsync(string path, string? token)
    {
        var client = TestEndpointsStartupFilter.AddTo(factory).CreateClient();
        if (token is not null)
        {
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }
        return await client.GetAsync(path);
    }

    private static async Task AssertErrorAsync(HttpResponseMessage response, HttpStatusCode status, string code)
    {
        Assert.Equal(status, response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = json.RootElement;
        Assert.Equal(code, root.GetProperty("code").GetString());
        Assert.Equal((int)status, root.GetProperty("status").GetInt32());
        Assert.False(root.GetProperty("success").GetBoolean());
        Assert.True(response.Headers.Contains("X-Correlation-Id"));
    }
}
