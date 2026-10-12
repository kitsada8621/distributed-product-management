using System.Net;
using System.Text.Json;
using Auth.IntegrationTests.Support;

namespace Auth.IntegrationTests.Endpoints;

public class LogoutEndpointTests(AuthApiFactory factory) : IClassFixture<AuthApiFactory>
{
    [Fact]
    public async Task Logout_ValidRequest_Returns200ClearsCookiesAndRevokesTheFamily()
    {
        var client = factory.CreateClient();
        var first = await AuthTestClient.RegisterAndLoginAsync(client, "logout.ok@example.com");
        var rotated = await AuthTestClient.ReadSessionAsync(await AuthTestClient.RefreshAsync(client, first));

        var response = await LogoutAsync(client, rotated);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(JsonValueKind.Null, json.RootElement.GetProperty("data").ValueKind);
        AssertCookieCleared(response, "refreshToken");
        AssertCookieCleared(response, "csrfToken");

        var afterLogout = await AuthTestClient.RefreshAsync(client, rotated);
        Assert.Equal(HttpStatusCode.Unauthorized, afterLogout.StatusCode);
        using var refreshJson = JsonDocument.Parse(await afterLogout.Content.ReadAsStringAsync());
        Assert.Equal("REFRESH_INVALID", refreshJson.RootElement.GetProperty("code").GetString());
    }

    [Fact]
    public async Task Logout_WithoutBearer_Returns401()
    {
        var client = factory.CreateClient();
        var session = await AuthTestClient.RegisterAndLoginAsync(client, "logout.nobearer@example.com");

        var response = await AuthTestClient.PostWithCookiesAsync(
            client, "/api/auth/logout", session.RefreshToken, session.CsrfToken, session.CsrfToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData("missing-header")]
    [InlineData("bad-origin")]
    public async Task Logout_BadCsrfOrOrigin_Returns403AndRevokesNothing(string scenario)
    {
        var client = factory.CreateClient();
        var session = await AuthTestClient.RegisterAndLoginAsync(client, $"logout.{scenario}@example.com");
        var csrfHeader = scenario == "missing-header" ? null : session.CsrfToken;
        var origin = scenario == "bad-origin" ? "http://evil.example.com" : AuthApiFactory.SpaOrigin;

        var response = await AuthTestClient.PostWithCookiesAsync(
            client, "/api/auth/logout", session.RefreshToken, session.CsrfToken, csrfHeader, origin, session.AccessToken);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        var stillWorks = await AuthTestClient.RefreshAsync(client, session);
        Assert.Equal(HttpStatusCode.OK, stillWorks.StatusCode);
    }

    [Fact]
    public async Task Logout_RefreshCookieOfAnotherUser_Returns403()
    {
        var client = factory.CreateClient();
        var mine = await AuthTestClient.RegisterAndLoginAsync(client, "logout.mine@example.com");
        var other = await AuthTestClient.RegisterAndLoginAsync(client, "logout.other@example.com");

        var response = await AuthTestClient.PostWithCookiesAsync(
            client, "/api/auth/logout", other.RefreshToken, other.CsrfToken, other.CsrfToken,
            bearer: mine.AccessToken);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        var otherStillWorks = await AuthTestClient.RefreshAsync(client, other);
        Assert.Equal(HttpStatusCode.OK, otherStillWorks.StatusCode);
    }

    [Fact]
    public async Task Logout_AccessTokenStaysValidUntilItExpires()
    {
        var client = factory.CreateClient();
        var session = await AuthTestClient.RegisterAndLoginAsync(client, "logout.stateless@example.com");
        await LogoutAsync(client, session);

        var secureClient = TestEndpointsStartupFilter.AddTo(factory).CreateClient();
        secureClient.DefaultRequestHeaders.Authorization = new("Bearer", session.AccessToken);
        var response = await secureClient.GetAsync("/test/secure");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private static Task<HttpResponseMessage> LogoutAsync(HttpClient client, LoginSession session)
    {
        return AuthTestClient.PostWithCookiesAsync(
            client, "/api/auth/logout", session.RefreshToken, session.CsrfToken, session.CsrfToken,
            bearer: session.AccessToken);
    }

    private static void AssertCookieCleared(HttpResponseMessage response, string name)
    {
        var header = AuthTestClient.SetCookieHeader(response, name);
        Assert.NotNull(header);
        Assert.Equal(string.Empty, AuthTestClient.CookieValue(response, name));
        Assert.Contains("1970", header);
    }
}
