using System.Net;
using Catalog.IntegrationTests.Support;

namespace Catalog.IntegrationTests.Endpoints;

public class HealthEndpointTests(CatalogApiFactory factory) : IClassFixture<CatalogApiFactory>
{
    [Fact]
    public async Task Health_ReturnsOkWithStatusOk()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/catalog/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("\"status\":\"ok\"", body);
    }
}
