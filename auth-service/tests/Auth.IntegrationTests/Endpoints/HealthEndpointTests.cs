using System.Net;
using Auth.IntegrationTests.Support;

namespace Auth.IntegrationTests.Endpoints;

public class HealthEndpointTests(AuthApiFactory factory) : IClassFixture<AuthApiFactory>
{
    [Fact]
    public async Task Health_ReturnsOkWithStatusOk()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/auth/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("\"status\":\"ok\"", body);
    }
}
