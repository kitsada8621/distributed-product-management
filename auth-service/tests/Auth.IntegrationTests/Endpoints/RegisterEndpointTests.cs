using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Auth.Domain.Users;
using Auth.Infrastructure.Persistence;
using Auth.IntegrationTests.Support;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Auth.IntegrationTests.Endpoints;

public class RegisterEndpointTests(AuthApiFactory factory) : IClassFixture<AuthApiFactory>
{
    [Fact]
    public async Task Register_ValidBody_Returns201WithUserRoleAndNoToken()
    {
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/register",
            new { email = "new.user@example.com", password = "Passw0rd!", name = "Somchai" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = json.RootElement;
        Assert.True(root.GetProperty("success").GetBoolean());
        var data = root.GetProperty("data");
        Assert.Equal("user", data.GetProperty("role").GetString());
        Assert.Equal("new.user@example.com", data.GetProperty("email").GetString());
        Assert.False(data.TryGetProperty("accessToken", out _));

        var stored = await FindUserAsync("new.user@example.com");
        Assert.Equal(data.GetProperty("id").GetGuid(), stored.Id);
        Assert.StartsWith("argon2id$", stored.PasswordHash);
    }

    [Fact]
    public async Task Register_DuplicateEmailWithDifferentCase_Returns409()
    {
        var client = factory.CreateClient();
        await client.PostAsJsonAsync("/api/auth/register",
            new { email = "dup@example.com", password = "Passw0rd!", name = "First" });

        var response = await client.PostAsJsonAsync("/api/auth/register",
            new { email = "DUP@Example.com", password = "Passw0rd!", name = "Second" });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("EMAIL_EXISTS", json.RootElement.GetProperty("code").GetString());
    }

    [Fact]
    public async Task Register_InvalidFields_Returns422WithFieldErrors()
    {
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/register",
            new { email = "not-an-email", password = "short", name = "" });

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("VALIDATION_ERROR", json.RootElement.GetProperty("code").GetString());
        var fields = json.RootElement.GetProperty("errors").EnumerateArray()
            .Select(e => e.GetProperty("field").GetString())
            .ToList();
        Assert.Equal(["email", "password", "name"], fields);
    }

    [Fact]
    public async Task Register_BodyWithRoleAdmin_StillCreatesRoleUser()
    {
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/register",
            new { email = "sneaky@example.com", password = "Passw0rd!", name = "Sneaky", role = "admin" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal(UserRole.User, (await FindUserAsync("sneaky@example.com")).Role);
    }

    [Fact]
    public async Task Register_MalformedJson_Returns400()
    {
        var client = factory.CreateClient();
        var content = new StringContent("{ not json", Encoding.UTF8, "application/json");

        var response = await client.PostAsync("/api/auth/register", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private async Task<User> FindUserAsync(string email)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
        return await context.Users.AsNoTracking().SingleAsync(u => u.Email == email);
    }
}
