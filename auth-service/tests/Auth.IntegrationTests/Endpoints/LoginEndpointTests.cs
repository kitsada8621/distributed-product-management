using System.Net;
using System.Text.Json;
using Auth.IntegrationTests.Support;
using Microsoft.IdentityModel.JsonWebTokens;

namespace Auth.IntegrationTests.Endpoints;

public class LoginEndpointTests(AuthApiFactory factory) : IClassFixture<AuthApiFactory>
{
    [Fact]
    public async Task Login_Success_ReturnsAccessTokenWithUserRoleAndFifteenMinuteLifetime()
    {
        var client = factory.CreateClient();
        await AuthTestClient.RegisterAsync(client, "login.ok@example.com");

        var response = await AuthTestClient.LoginAsync(client, "login.ok@example.com");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var session = await AuthTestClient.ReadSessionAsync(response);
        var token = new JsonWebToken(session.AccessToken);
        Assert.Equal("user", token.GetClaim("role").Value);
        Assert.Equal("login.ok@example.com", token.GetClaim("email").Value);
        Assert.InRange((token.ValidTo - DateTime.UtcNow).TotalMinutes, 14, 15.1);
    }

    [Fact]
    public async Task Login_Success_SetsRefreshAndCsrfCookiesWithTheDocumentedAttributes()
    {
        var client = factory.CreateClient();
        await AuthTestClient.RegisterAsync(client, "login.cookies@example.com");

        var response = await AuthTestClient.LoginAsync(client, "login.cookies@example.com");

        var refresh = AuthTestClient.SetCookieHeader(response, "refreshToken")!.ToLowerInvariant();
        Assert.Contains("httponly", refresh);
        Assert.Contains("secure", refresh);
        Assert.Contains("samesite=strict", refresh);
        Assert.Contains("path=/api/auth", refresh);
        var csrf = AuthTestClient.SetCookieHeader(response, "csrfToken")!.ToLowerInvariant();
        Assert.DoesNotContain("httponly", csrf);
        Assert.Contains("path=/;", csrf + ";");
    }

    [Fact]
    public async Task Login_WithCookieSettingsFromConfiguration_UsesThemForTheRefreshCookie()
    {
        await using var configured = factory.WithWebHostBuilder(builder => builder
            .UseSetting("AuthCookie:Secure", "false")
            .UseSetting("AuthCookie:SameSite", "Lax"));
        var client = configured.CreateClient();
        await AuthTestClient.RegisterAsync(client, "login.cookiecfg@example.com");

        var response = await AuthTestClient.LoginAsync(client, "login.cookiecfg@example.com");

        var refresh = AuthTestClient.SetCookieHeader(response, "refreshToken")!.ToLowerInvariant();
        Assert.Contains("httponly", refresh);
        Assert.DoesNotContain("secure", refresh);
        Assert.Contains("samesite=lax", refresh);
    }

    [Fact]
    public async Task Login_Success_NeverPutsTheRefreshTokenInTheBody()
    {
        var client = factory.CreateClient();
        await AuthTestClient.RegisterAsync(client, "login.body@example.com");

        var response = await AuthTestClient.LoginAsync(client, "login.body@example.com");

        var refreshToken = AuthTestClient.CookieValue(response, "refreshToken")!;
        Assert.DoesNotContain(refreshToken, await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Login_SeededAdmin_GetsAdminRole()
    {
        var client = factory.CreateClient();

        var session = await AuthTestClient.LoginSessionAsync(client, AuthApiFactory.AdminEmail, AuthApiFactory.AdminPassword);

        Assert.Equal("admin", new JsonWebToken(session.AccessToken).GetClaim("role").Value);
    }

    [Fact]
    public async Task Login_UnknownEmailAndWrongPassword_AreIndistinguishable()
    {
        var client = factory.CreateClient();
        await AuthTestClient.RegisterAsync(client, "login.same@example.com");

        var unknown = await AuthTestClient.LoginAsync(client, "nobody@example.com");
        var wrong = await AuthTestClient.LoginAsync(client, "login.same@example.com", "wrong-password");

        Assert.Equal(HttpStatusCode.Unauthorized, unknown.StatusCode);
        Assert.Equal(unknown.StatusCode, wrong.StatusCode);
        Assert.Equal(await ErrorSummaryAsync(unknown), await ErrorSummaryAsync(wrong));
        Assert.Contains("INVALID_CREDENTIALS", await ErrorSummaryAsync(wrong));
    }

    [Fact]
    public async Task Login_FiveWrongPasswords_LockTheAccountEvenForTheRightPassword()
    {
        var client = factory.CreateClient();
        await AuthTestClient.RegisterAsync(client, "login.lock@example.com");

        for (var i = 0; i < 4; i++)
        {
            var failed = await AuthTestClient.LoginAsync(client, "login.lock@example.com", "wrong-password");
            Assert.Equal(HttpStatusCode.Unauthorized, failed.StatusCode);
        }
        var fifth = await AuthTestClient.LoginAsync(client, "login.lock@example.com", "wrong-password");
        var afterLock = await AuthTestClient.LoginAsync(client, "login.lock@example.com");

        Assert.Equal(HttpStatusCode.Locked, fifth.StatusCode);
        Assert.Equal(HttpStatusCode.Locked, afterLock.StatusCode);
        using var json = JsonDocument.Parse(await afterLock.Content.ReadAsStringAsync());
        Assert.Equal("ACCOUNT_LOCKED", json.RootElement.GetProperty("code").GetString());
        Assert.True(json.RootElement.GetProperty("data").GetProperty("lockedUntil").GetDateTime() > DateTime.UtcNow);
    }

    [Fact]
    public async Task Login_SuccessAfterThreeFailures_ResetsTheCounter()
    {
        var client = factory.CreateClient();
        await AuthTestClient.RegisterAsync(client, "login.reset@example.com");
        for (var i = 0; i < 3; i++)
        {
            await AuthTestClient.LoginAsync(client, "login.reset@example.com", "wrong-password");
        }
        var success = await AuthTestClient.LoginAsync(client, "login.reset@example.com");
        Assert.Equal(HttpStatusCode.OK, success.StatusCode);

        for (var i = 0; i < 4; i++)
        {
            var failed = await AuthTestClient.LoginAsync(client, "login.reset@example.com", "wrong-password");
            Assert.Equal(HttpStatusCode.Unauthorized, failed.StatusCode);
        }

        var stillOpen = await AuthTestClient.LoginAsync(client, "login.reset@example.com");
        Assert.Equal(HttpStatusCode.OK, stillOpen.StatusCode);
    }

    [Fact]
    public async Task Login_EmptyFields_Returns422()
    {
        var client = factory.CreateClient();

        var response = await AuthTestClient.LoginAsync(client, "", "");

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    private static async Task<string> ErrorSummaryAsync(HttpResponseMessage response)
    {
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = json.RootElement;
        return $"{root.GetProperty("status").GetInt32()}|{root.GetProperty("code").GetString()}|{root.GetProperty("message").GetString()}";
    }
}
