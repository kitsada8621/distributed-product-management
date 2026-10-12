using Catalog.Infrastructure.Persistence;
using Catalog.IntegrationTests.Support;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Catalog.IntegrationTests.Endpoints;

public class StartupTests(CatalogApiFactory factory) : IClassFixture<CatalogApiFactory>
{
    [Fact]
    public async Task Startup_CreatesTheDatabaseFileAndAppliesInitialCreate()
    {
        using var client = factory.CreateClient();
        await client.GetAsync("/api/catalog/health");

        Assert.True(File.Exists(factory.DatabasePath));
        await using var scope = factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
        var applied = await context.Database.GetAppliedMigrationsAsync();
        Assert.Contains(applied, name => name.EndsWith("_InitialCreate"));
    }

    [Fact]
    public async Task Startup_CreatesTheCategoryAndProductTables()
    {
        using var client = factory.CreateClient();

        await using var scope = factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
        Assert.Empty(await context.Categories.ToListAsync());
        Assert.Empty(await context.Products.ToListAsync());
    }
}
