using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Auth.Infrastructure.Persistence;
using Auth.IntegrationTests.Support;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Auth.IntegrationTests.Endpoints;

public class ForgotPasswordEndpointTests(AuthApiFactory factory) : IClassFixture<AuthApiFactory>
{
    [Fact]
    public async Task ForgotPassword_KnownAndUnknownEmail_GiveTheSameResponse()
    {
        var client = factory.CreateClient();
        await AuthTestClient.RegisterAsync(client, "forgot.known@example.com");

        var known = await PostForgotAsync(client, "forgot.known@example.com");
        var unknown = await PostForgotAsync(client, "forgot.unknown@example.com");

        Assert.Equal(HttpStatusCode.Accepted, known.StatusCode);
        Assert.Equal(await StableBodyAsync(known), await StableBodyAsync(unknown));
        Assert.Contains("data=null", await StableBodyAsync(known));
    }

    [Fact]
    public async Task ForgotPassword_KnownEmail_StoresHashedTokenAndOneOutboxRow()
    {
        var client = factory.CreateClient();
        await AuthTestClient.RegisterAsync(client, "forgot.rows@example.com");

        await PostForgotAsync(client, "forgot.rows@example.com");

        await using var scope = factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
        var user = await context.Users.SingleAsync(u => u.Email == "forgot.rows@example.com");
        var resetToken = await context.ResetTokens.SingleAsync(t => t.UserId == user.Id);
        var email = await context.EmailOutboxes.SingleAsync(e => e.UserId == user.Id);
        var rawToken = ExtractToken(email.Body);
        Assert.Contains($"{AuthApiFactory.ResetBaseUrl}?token={rawToken}", email.Body);
        Assert.NotEqual(rawToken, resetToken.TokenHash);
        Assert.Equal(64, resetToken.TokenHash.Length);
    }

    [Fact]
    public async Task ForgotPassword_UnknownEmail_CreatesNothing()
    {
        var client = factory.CreateClient();

        await PostForgotAsync(client, "forgot.nobody@example.com");

        await using var scope = factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
        Assert.False(await context.EmailOutboxes.AnyAsync(e => e.ToEmail == "forgot.nobody@example.com"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-an-email")]
    public async Task ForgotPassword_InvalidEmail_Returns422(string email)
    {
        var client = factory.CreateClient();

        var response = await PostForgotAsync(client, email);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task ForgotPassword_WorkerDeliversTheEmailToTheUser()
    {
        var client = factory.CreateClient();
        await AuthTestClient.RegisterAsync(client, "forgot.deliver@example.com");

        await PostForgotAsync(client, "forgot.deliver@example.com");

        await WaitUntilAsync(() => factory.EmailSender.Sent.Any(e => e.To == "forgot.deliver@example.com"));
        var sent = factory.EmailSender.Sent.First(e => e.To == "forgot.deliver@example.com");
        Assert.Contains($"{AuthApiFactory.ResetBaseUrl}?token=", sent.Body);
    }

    public static string ExtractToken(string body)
    {
        const string marker = "?token=";
        return body[(body.IndexOf(marker, StringComparison.Ordinal) + marker.Length)..].Trim();
    }

    private static Task<HttpResponseMessage> PostForgotAsync(HttpClient client, string email)
    {
        return client.PostAsJsonAsync("/api/auth/forgot-password", new { email });
    }

    private static async Task<string> StableBodyAsync(HttpResponseMessage response)
    {
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = json.RootElement;
        return $"{root.GetProperty("status")}|{root.GetProperty("success")}|{root.GetProperty("code")}|"
            + $"{root.GetProperty("message")}|data={(root.GetProperty("data").ValueKind == JsonValueKind.Null ? "null" : "set")}";
    }

    private static async Task WaitUntilAsync(Func<bool> condition)
    {
        var deadline = DateTime.UtcNow.AddSeconds(15);
        while (DateTime.UtcNow < deadline)
        {
            if (condition())
            {
                return;
            }

            await Task.Delay(250);
        }

        Assert.Fail("Condition was not met within 15 seconds");
    }
}
