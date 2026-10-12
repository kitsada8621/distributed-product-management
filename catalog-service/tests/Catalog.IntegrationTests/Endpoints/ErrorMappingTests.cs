using System.Net;
using System.Text.Json;
using Catalog.IntegrationTests.Support;

namespace Catalog.IntegrationTests.Endpoints;

public class ErrorMappingTests(CatalogApiFactory factory) : IClassFixture<CatalogApiFactory>
{
    [Theory]
    [InlineData("VALIDATION_ERROR", 422)]
    [InlineData("STOCK_NEGATIVE", 422)]
    [InlineData("UNAUTHORIZED", 401)]
    [InlineData("FORBIDDEN", 403)]
    [InlineData("NOT_FOUND", 404)]
    [InlineData("CATEGORY_NAME_EXISTS", 409)]
    public async Task CatalogException_MapsToTheDocumentedStatusAndCode(string code, int expectedStatus)
    {
        var client = TestEndpointsStartupFilter.AddTo(factory).CreateClient();

        var response = await client.GetAsync($"/test/throw-{code}");

        Assert.Equal(expectedStatus, (int)response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = json.RootElement;
        Assert.Equal(code, root.GetProperty("code").GetString());
        Assert.Equal(expectedStatus, root.GetProperty("status").GetInt32());
        Assert.False(root.GetProperty("success").GetBoolean());
    }

    [Fact]
    public async Task ValidationError_CarriesFieldErrors()
    {
        var client = TestEndpointsStartupFilter.AddTo(factory).CreateClient();

        var response = await client.GetAsync("/test/throw-VALIDATION_ERROR");

        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var error = json.RootElement.GetProperty("errors")[0];
        Assert.Equal("name", error.GetProperty("field").GetString());
        Assert.Equal("REQUIRED", error.GetProperty("code").GetString());
        Assert.Equal("Name is required", error.GetProperty("message").GetString());
    }

    [Fact]
    public async Task ErrorWithoutFieldErrors_HasNullErrors()
    {
        var client = TestEndpointsStartupFilter.AddTo(factory).CreateClient();

        var response = await client.GetAsync("/test/throw-NOT_FOUND");

        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(JsonValueKind.Null, json.RootElement.GetProperty("errors").ValueKind);
    }

    [Fact]
    public async Task UnknownCode_FallsThroughToInternalError()
    {
        var client = TestEndpointsStartupFilter.AddTo(factory).CreateClient();

        var response = await client.GetAsync("/test/throw-SOMETHING_NEW");

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("INTERNAL_ERROR", json.RootElement.GetProperty("code").GetString());
    }
}
