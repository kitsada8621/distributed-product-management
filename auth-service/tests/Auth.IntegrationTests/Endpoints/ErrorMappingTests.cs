using System.Net;
using System.Text.Json;
using Auth.IntegrationTests.Support;

namespace Auth.IntegrationTests.Endpoints;

public class ErrorMappingTests(AuthApiFactory factory) : IClassFixture<AuthApiFactory>
{
    [Theory]
    [InlineData("VALIDATION_ERROR", 422)]
    [InlineData("INVALID_CREDENTIALS", 401)]
    [InlineData("REFRESH_INVALID", 401)]
    [InlineData("REFRESH_REUSED", 401)]
    [InlineData("UNAUTHORIZED", 401)]
    [InlineData("CSRF_INVALID", 403)]
    [InlineData("NOT_FOUND", 404)]
    [InlineData("EMAIL_EXISTS", 409)]
    [InlineData("RESET_TOKEN_INVALID", 400)]
    [InlineData("ACCOUNT_LOCKED", 423)]
    public async Task AuthException_MapsToTheDocumentedStatusAndCode(string code, int expectedStatus)
    {
        var client = TestEndpointsStartupFilter.AddTo(factory).CreateClient();

        var response = await client.GetAsync($"/test/throw/{code}");

        Assert.Equal(expectedStatus, (int)response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = json.RootElement;
        Assert.Equal(code, root.GetProperty("code").GetString());
        Assert.Equal(expectedStatus, root.GetProperty("status").GetInt32());
        Assert.False(root.GetProperty("success").GetBoolean());
        Assert.False(string.IsNullOrEmpty(root.GetProperty("correlationId").GetString()));
    }

    [Fact]
    public async Task ValidationError_CarriesFieldErrors()
    {
        var client = TestEndpointsStartupFilter.AddTo(factory).CreateClient();

        var response = await client.GetAsync("/test/throw/VALIDATION_ERROR");

        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var error = json.RootElement.GetProperty("errors")[0];
        Assert.Equal("email", error.GetProperty("field").GetString());
        Assert.Equal("INVALID_FORMAT", error.GetProperty("code").GetString());
        Assert.Equal("Invalid email format", error.GetProperty("message").GetString());
    }

    [Fact]
    public async Task AccountLocked_CarriesLockedUntilInData()
    {
        var client = TestEndpointsStartupFilter.AddTo(factory).CreateClient();

        var response = await client.GetAsync("/test/throw/ACCOUNT_LOCKED");

        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.True(json.RootElement.GetProperty("data").TryGetProperty("lockedUntil", out var lockedUntil));
        Assert.True(lockedUntil.GetDateTime() > DateTime.UtcNow);
        Assert.Equal(JsonValueKind.Null, json.RootElement.GetProperty("errors").ValueKind);
    }

    [Fact]
    public async Task UnknownCode_FallsThroughToInternalError()
    {
        var client = TestEndpointsStartupFilter.AddTo(factory).CreateClient();

        var response = await client.GetAsync("/test/throw/SOMETHING_NEW");

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("INTERNAL_ERROR", json.RootElement.GetProperty("code").GetString());
    }
}
