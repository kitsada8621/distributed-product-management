using System.Net;
using System.Text;
using System.Text.Json;
using Catalog.IntegrationTests.Support;

namespace Catalog.IntegrationTests.Endpoints;

public class EnvelopeTests(CatalogApiFactory factory) : IClassFixture<CatalogApiFactory>
{
    private static readonly string[] EnvelopeKeys =
    [
        "data", "code", "errors", "message", "meta", "status", "success"
    ];

    [Fact]
    public async Task Health_ReturnsExactlyTheEnvelopeKeys()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/catalog/health");

        using var json = await ReadJsonAsync(response);
        var root = json.RootElement;
        var actualKeys = root.EnumerateObject().Select(property => property.Name).Order().ToArray();
        Assert.Equal(EnvelopeKeys.Order().ToArray(), actualKeys);
        Assert.Equal(200, root.GetProperty("status").GetInt32());
        Assert.True(root.GetProperty("success").GetBoolean());
        Assert.Equal("SUCCESS", root.GetProperty("code").GetString());
        Assert.Equal(JsonValueKind.Null, root.GetProperty("meta").ValueKind);
        Assert.Equal(JsonValueKind.Null, root.GetProperty("errors").ValueKind);
        Assert.Equal("ok", root.GetProperty("data").GetProperty("status").GetString());
    }

    [Fact]
    public async Task MissingCorrelationHeader_GeneratesGuidInHeader()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/catalog/health");

        var headerValue = response.Headers.GetValues("X-Correlation-Id").Single();
        Assert.True(Guid.TryParse(headerValue, out _));
    }

    [Fact]
    public async Task ProvidedCorrelationHeader_IsEchoedInHeader()
    {
        var client = factory.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/catalog/health");
        request.Headers.Add("X-Correlation-Id", "abc-123");

        var response = await client.SendAsync(request);

        Assert.Equal("abc-123", response.Headers.GetValues("X-Correlation-Id").Single());
    }

    [Fact]
    public async Task UnhandledException_Returns500WithGenericMessage()
    {
        var client = TestEndpointsStartupFilter.AddTo(factory).CreateClient();

        var response = await client.GetAsync("/test/throw");

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("secret failure detail", body);
        using var json = JsonDocument.Parse(body);
        Assert.Equal("INTERNAL_ERROR", json.RootElement.GetProperty("code").GetString());
        Assert.False(json.RootElement.GetProperty("success").GetBoolean());
    }

    [Fact]
    public async Task MalformedJsonBody_Returns400BadRequest()
    {
        var client = TestEndpointsStartupFilter.AddTo(factory).CreateClient();
        var content = new StringContent("{ not json", Encoding.UTF8, "application/json");

        var response = await client.PostAsync("/test/body", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var json = await ReadJsonAsync(response);
        Assert.Equal("BAD_REQUEST", json.RootElement.GetProperty("code").GetString());
        Assert.Equal(400, json.RootElement.GetProperty("status").GetInt32());
    }

    private static async Task<JsonDocument> ReadJsonAsync(HttpResponseMessage response)
    {
        return JsonDocument.Parse(await response.Content.ReadAsStringAsync());
    }
}
