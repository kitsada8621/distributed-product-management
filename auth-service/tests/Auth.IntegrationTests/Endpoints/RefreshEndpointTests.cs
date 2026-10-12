using System.Net;
using System.Text.Json;
using Auth.IntegrationTests.Support;

namespace Auth.IntegrationTests.Endpoints;

public class RefreshEndpointTests(AuthApiFactory factory) : IClassFixture<AuthApiFactory>
{
    [Fact]
    public async Task Refresh_ValidRequest_RotatesCookiesAndReturnsNewAccessToken()
    {
        var client = factory.CreateClient();
        var session = await AuthTestClient.RegisterAndLoginAsync(client, "refresh.ok@example.com");

        var response = await AuthTestClient.RefreshAsync(client, session);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var renewed = await AuthTestClient.ReadSessionAsync(response);
        Assert.NotEqual(session.RefreshToken, renewed.RefreshToken);
        Assert.NotEqual(session.CsrfToken, renewed.CsrfToken);
        Assert.NotEmpty(renewed.AccessToken);

        var again = await AuthTestClient.RefreshAsync(client, renewed);
        Assert.Equal(HttpStatusCode.OK, again.StatusCode);
    }

    [Fact]
    public async Task Refresh_ReusingTheOldCookie_Returns401ReusedAndRevokesTheFamily()
    {
        var client = factory.CreateClient();
        var session = await AuthTestClient.RegisterAndLoginAsync(client, "refresh.reuse@example.com");
        var renewed = await AuthTestClient.ReadSessionAsync(await AuthTestClient.RefreshAsync(client, session));

        var reuse = await AuthTestClient.RefreshAsync(client, session);
        var afterReuse = await AuthTestClient.RefreshAsync(client, renewed);

        Assert.Equal(HttpStatusCode.Unauthorized, reuse.StatusCode);
        Assert.Equal("REFRESH_REUSED", await CodeAsync(reuse));
        Assert.Equal(HttpStatusCode.Unauthorized, afterReuse.StatusCode);
        Assert.Equal("REFRESH_INVALID", await CodeAsync(afterReuse));
    }

    [Fact]
    public async Task Refresh_MissingCsrfHeader_Returns403()
    {
        var client = factory.CreateClient();
        var session = await AuthTestClient.RegisterAndLoginAsync(client, "refresh.nocsrf@example.com");

        var response = await AuthTestClient.PostWithCookiesAsync(
            client, "/api/auth/refresh", session.RefreshToken, session.CsrfToken, csrfHeader: null);

        await AssertCsrfInvalidAsync(response);
    }

    [Fact]
    public async Task Refresh_HeaderDifferentFromCookie_Returns403()
    {
        var client = factory.CreateClient();
        var session = await AuthTestClient.RegisterAndLoginAsync(client, "refresh.mismatch@example.com");

        var response = await AuthTestClient.PostWithCookiesAsync(
            client, "/api/auth/refresh", session.RefreshToken, session.CsrfToken, csrfHeader: "something-else");

        await AssertCsrfInvalidAsync(response);
    }

    [Fact]
    public async Task Refresh_CsrfTokenFromAnotherSession_Returns403()
    {
        var client = factory.CreateClient();
        var mine = await AuthTestClient.RegisterAndLoginAsync(client, "refresh.mine@example.com");
        var other = await AuthTestClient.RegisterAndLoginAsync(client, "refresh.other@example.com");

        var response = await AuthTestClient.PostWithCookiesAsync(
            client, "/api/auth/refresh", mine.RefreshToken, other.CsrfToken, other.CsrfToken);

        await AssertCsrfInvalidAsync(response);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("http://evil.example.com")]
    public async Task Refresh_MissingOrWrongOrigin_Returns403(string? origin)
    {
        var client = factory.CreateClient();
        var session = await AuthTestClient.RegisterAndLoginAsync(client, $"refresh.origin{origin?.Length}@example.com");

        var response = await AuthTestClient.PostWithCookiesAsync(
            client, "/api/auth/refresh", session.RefreshToken, session.CsrfToken, session.CsrfToken, origin);

        await AssertCsrfInvalidAsync(response);
    }

    [Fact]
    public async Task Refresh_NoRefreshCookie_Returns401Invalid()
    {
        var client = factory.CreateClient();

        var response = await AuthTestClient.PostWithCookiesAsync(
            client, "/api/auth/refresh", refreshCookie: null, csrfCookie: "x", csrfHeader: "x");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("REFRESH_INVALID", await CodeAsync(response));
    }

    [Fact]
    public async Task Refresh_BadCsrfOnAnAlreadyUsedToken_DoesNotRevokeTheFamily()
    {
        var client = factory.CreateClient();
        var session = await AuthTestClient.RegisterAndLoginAsync(client, "refresh.safe@example.com");
        var renewed = await AuthTestClient.ReadSessionAsync(await AuthTestClient.RefreshAsync(client, session));

        var forged = await AuthTestClient.PostWithCookiesAsync(
            client, "/api/auth/refresh", session.RefreshToken, "bad", "bad");

        await AssertCsrfInvalidAsync(forged);
        var stillWorks = await AuthTestClient.RefreshAsync(client, renewed);
        Assert.Equal(HttpStatusCode.OK, stillWorks.StatusCode);
    }

    private static async Task AssertCsrfInvalidAsync(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal("CSRF_INVALID", await CodeAsync(response));
    }

    private static async Task<string?> CodeAsync(HttpResponseMessage response)
    {
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return json.RootElement.GetProperty("code").GetString();
    }
}
