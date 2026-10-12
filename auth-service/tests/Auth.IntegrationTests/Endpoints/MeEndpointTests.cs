using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Auth.Domain.Users;
using Auth.Infrastructure.Persistence;
using Auth.Infrastructure.Security;
using Auth.IntegrationTests.Support;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Auth.IntegrationTests.Endpoints;

public class MeEndpointTests(AuthApiFactory factory) : IClassFixture<AuthApiFactory>
{
    [Fact]
    public async Task Me_AfterLogin_ReturnsIdEmailNameAndRole()
    {
        var client = factory.CreateClient();
        var session = await AuthTestClient.RegisterAndLoginAsync(client, "me.ok@example.com");

        var response = await GetMeAsync(client, session.AccessToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var data = json.RootElement.GetProperty("data");
        Assert.Equal("me.ok@example.com", data.GetProperty("email").GetString());
        Assert.Equal("Somchai", data.GetProperty("name").GetString());
        Assert.Equal("user", data.GetProperty("role").GetString());
        Assert.Equal((await FindUserAsync("me.ok@example.com"))!.Id, data.GetProperty("id").GetGuid());
    }

    [Fact]
    public async Task Me_SeededAdmin_ReturnsAdminRole()
    {
        var client = factory.CreateClient();
        var session = await AuthTestClient.LoginSessionAsync(client, AuthApiFactory.AdminEmail, AuthApiFactory.AdminPassword);

        var response = await GetMeAsync(client, session.AccessToken);

        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("admin", json.RootElement.GetProperty("data").GetProperty("role").GetString());
    }

    [Fact]
    public async Task Me_RoleChangedInDatabase_ShowsTheNewRoleWithTheOldToken()
    {
        var client = factory.CreateClient();
        var session = await AuthTestClient.RegisterAndLoginAsync(client, "me.promoted@example.com");
        await ExecuteSqlAsync("UPDATE users SET role = 'admin' WHERE email = 'me.promoted@example.com'");

        var response = await GetMeAsync(client, session.AccessToken);

        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("admin", json.RootElement.GetProperty("data").GetProperty("role").GetString());
    }

    [Fact]
    public async Task Me_WithoutToken_Returns401()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/auth/me");

        await AssertUnauthorizedAsync(response);
    }

    [Fact]
    public async Task Me_ExpiredToken_Returns401()
    {
        var client = factory.CreateClient();
        await AuthTestClient.RegisterAsync(client, "me.expired@example.com");
        var user = (await FindUserAsync("me.expired@example.com"))!;
        var issuer = new JwtAccessTokenIssuer(
            new JwtOptions { Issuer = "auth-service-tests", PrivateKeyPath = factory.KeyPath },
            new PastTimeProvider());

        var response = await GetMeAsync(client, issuer.Issue(user));

        await AssertUnauthorizedAsync(response);
    }

    [Fact]
    public async Task Me_UserDeletedAfterTokenWasIssued_Returns401()
    {
        var client = factory.CreateClient();
        var session = await AuthTestClient.RegisterAndLoginAsync(client, "me.deleted@example.com");
        await ExecuteSqlAsync("DELETE FROM users WHERE email = 'me.deleted@example.com'");

        var response = await GetMeAsync(client, session.AccessToken);

        await AssertUnauthorizedAsync(response);
    }

    private static Task<HttpResponseMessage> GetMeAsync(HttpClient client, string accessToken)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/auth/me");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return client.SendAsync(request);
    }

    private static async Task AssertUnauthorizedAsync(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("UNAUTHORIZED", json.RootElement.GetProperty("code").GetString());
    }

    private async Task<User?> FindUserAsync(string email)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
        return await context.Users.AsNoTracking().SingleOrDefaultAsync(u => u.Email == email);
    }

    private async Task ExecuteSqlAsync(string sql)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
        await context.Database.ExecuteSqlRawAsync(sql);
    }

    private sealed class PastTimeProvider : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => DateTimeOffset.UtcNow.AddHours(-1);
    }
}
