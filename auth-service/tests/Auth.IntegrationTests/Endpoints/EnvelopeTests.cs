using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Auth.IntegrationTests.Support;
using Microsoft.Extensions.DependencyInjection;

namespace Auth.IntegrationTests.Endpoints;

public class EnvelopeTests(AuthApiFactory factory) : IClassFixture<AuthApiFactory>
{
    private static readonly string[] EnvelopeKeys =
    [
        "status", "success", "code", "message", "data", "meta", "errors", "correlationId", "timestamp"
    ];

    [Fact]
    public async Task Health_ReturnsAllEnvelopeKeys()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/auth/health");

        using var json = await ReadJsonAsync(response);
        var root = json.RootElement;
        foreach (var key in EnvelopeKeys)
        {
            Assert.True(root.TryGetProperty(key, out _), $"missing key {key}");
        }
        Assert.Equal(200, root.GetProperty("status").GetInt32());
        Assert.True(root.GetProperty("success").GetBoolean());
        Assert.Equal("SUCCESS", root.GetProperty("code").GetString());
        Assert.Equal(JsonValueKind.Null, root.GetProperty("meta").ValueKind);
        Assert.Equal(JsonValueKind.Null, root.GetProperty("errors").ValueKind);
        Assert.Equal("ok", root.GetProperty("data").GetProperty("status").GetString());
    }

    [Fact]
    public async Task MissingCorrelationHeader_GeneratesGuidInHeaderAndBody()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/auth/health");

        var headerValue = response.Headers.GetValues("X-Correlation-Id").Single();
        Assert.True(Guid.TryParse(headerValue, out _));
        using var json = await ReadJsonAsync(response);
        Assert.Equal(headerValue, json.RootElement.GetProperty("correlationId").GetString());
    }

    [Fact]
    public async Task ProvidedCorrelationHeader_IsEchoedInHeaderAndBody()
    {
        var client = factory.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/auth/health");
        request.Headers.Add("X-Correlation-Id", "abc-123");

        var response = await client.SendAsync(request);

        Assert.Equal("abc-123", response.Headers.GetValues("X-Correlation-Id").Single());
        using var json = await ReadJsonAsync(response);
        Assert.Equal("abc-123", json.RootElement.GetProperty("correlationId").GetString());
    }

    [Fact]
    public async Task UnhandledException_Returns500WithGenericMessage()
    {
        var client = CreateClientWithTestEndpoints();

        var response = await client.GetAsync("/test/throw");

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("secret failure detail", body);
        using var json = JsonDocument.Parse(body);
        Assert.Equal("INTERNAL_ERROR", json.RootElement.GetProperty("code").GetString());
        Assert.False(json.RootElement.GetProperty("success").GetBoolean());
        Assert.False(string.IsNullOrEmpty(json.RootElement.GetProperty("correlationId").GetString()));
    }

    [Fact]
    public async Task MalformedJsonBody_Returns400BadRequest()
    {
        var client = CreateClientWithTestEndpoints();
        var content = new StringContent("{ not json", Encoding.UTF8, "application/json");

        var response = await client.PostAsync("/test/body", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var json = await ReadJsonAsync(response);
        Assert.Equal("BAD_REQUEST", json.RootElement.GetProperty("code").GetString());
        Assert.Equal(400, json.RootElement.GetProperty("status").GetInt32());
    }

    private HttpClient CreateClientWithTestEndpoints()
    {
        return factory
            .WithWebHostBuilder(builder => builder.ConfigureServices(
                services => services.AddSingleton<IStartupFilter, TestEndpointsStartupFilter>()))
            .CreateClient();
    }

    private static async Task<JsonDocument> ReadJsonAsync(HttpResponseMessage response)
    {
        return JsonDocument.Parse(await response.Content.ReadAsStringAsync());
    }

    private sealed record TestBody(string Name);

    private sealed class TestEndpointsStartupFilter : IStartupFilter
    {
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
        {
            return app =>
            {
                next(app);
                app.UseRouting();
                app.UseEndpoints(endpoints =>
                {
                    endpoints.MapGet("/test/throw", IResult () => throw new InvalidOperationException("secret failure detail"));
                    endpoints.MapPost("/test/body", (TestBody body) => Results.Ok(body.Name));
                });
            };
        }
    }
}
