using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Auth.Infrastructure.Persistence;
using Auth.IntegrationTests.Support;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Auth.IntegrationTests.Endpoints;

public class ResetPasswordEndpointTests(AuthApiFactory factory) : IClassFixture<AuthApiFactory>
{
    private const string NewPassword = "NewPassw0rd!";

    [Fact]
    public async Task Reset_FullFlow_ChangesThePasswordAndInvalidatesOldSessions()
    {
        var client = factory.CreateClient();
        var oldSession = await AuthTestClient.RegisterAndLoginAsync(client, "reset.flow@example.com");
        var token = await RequestResetTokenAsync(client, "reset.flow@example.com");

        var response = await ResetAsync(client, token, NewPassword);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(JsonValueKind.Null, json.RootElement.GetProperty("data").ValueKind);
        var withNew = await AuthTestClient.LoginAsync(client, "reset.flow@example.com", NewPassword);
        var withOld = await AuthTestClient.LoginAsync(client, "reset.flow@example.com");
        Assert.Equal(HttpStatusCode.OK, withNew.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, withOld.StatusCode);
        var oldRefresh = await AuthTestClient.RefreshAsync(client, oldSession);
        Assert.Equal(HttpStatusCode.Unauthorized, oldRefresh.StatusCode);
        Assert.Equal("REFRESH_INVALID", await CodeAsync(oldRefresh));
    }

    [Fact]
    public async Task Reset_SameTokenTwice_SecondTimeReturns400()
    {
        var client = factory.CreateClient();
        await AuthTestClient.RegisterAsync(client, "reset.twice@example.com");
        var token = await RequestResetTokenAsync(client, "reset.twice@example.com");
        await ResetAsync(client, token, NewPassword);

        var second = await ResetAsync(client, token, "AnotherPassw0rd!");

        Assert.Equal(HttpStatusCode.BadRequest, second.StatusCode);
        Assert.Equal("RESET_TOKEN_INVALID", await CodeAsync(second));
    }

    [Fact]
    public async Task Reset_UnknownToken_Returns400()
    {
        var client = factory.CreateClient();

        var response = await ResetAsync(client, "this-token-does-not-exist", NewPassword);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("RESET_TOKEN_INVALID", await CodeAsync(response));
    }

    [Fact]
    public async Task Reset_ExpiredToken_Returns400()
    {
        var client = factory.CreateClient();
        await AuthTestClient.RegisterAsync(client, "reset.expired@example.com");
        var token = await RequestResetTokenAsync(client, "reset.expired@example.com");
        await ExpireResetTokensAsync("reset.expired@example.com");

        var response = await ResetAsync(client, token, NewPassword);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("RESET_TOKEN_INVALID", await CodeAsync(response));
    }

    [Fact]
    public async Task Reset_ShortPassword_Returns422AndKeepsTheTokenUsable()
    {
        var client = factory.CreateClient();
        await AuthTestClient.RegisterAsync(client, "reset.short@example.com");
        var token = await RequestResetTokenAsync(client, "reset.short@example.com");

        var tooShort = await ResetAsync(client, token, "short");
        var retry = await ResetAsync(client, token, NewPassword);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, tooShort.StatusCode);
        Assert.Equal(HttpStatusCode.OK, retry.StatusCode);
    }

    private async Task<string> RequestResetTokenAsync(HttpClient client, string email)
    {
        await client.PostAsJsonAsync("/api/auth/forgot-password", new { email });

        await using var scope = factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
        var outbox = await context.EmailOutboxes.AsNoTracking().SingleAsync(e => e.ToEmail == email);
        return ForgotPasswordEndpointTests.ExtractToken(outbox.Body);
    }

    private async Task ExpireResetTokensAsync(string email)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
        var user = await context.Users.AsNoTracking().SingleAsync(u => u.Email == email);
        await context.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE reset_tokens SET expires_at = '2020-01-01T00:00:00Z' WHERE user_id = {user.Id}");
    }

    private static Task<HttpResponseMessage> ResetAsync(HttpClient client, string token, string newPassword)
    {
        return client.PostAsJsonAsync("/api/auth/reset-password", new { token, newPassword });
    }

    private static async Task<string?> CodeAsync(HttpResponseMessage response)
    {
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return json.RootElement.GetProperty("code").GetString();
    }
}
