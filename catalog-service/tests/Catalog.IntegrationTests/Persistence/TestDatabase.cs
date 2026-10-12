using Catalog.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Catalog.IntegrationTests.Persistence;

public sealed class TestDatabase : IDisposable
{
    private readonly string _path = Path.Combine(Path.GetTempPath(), $"catalog-test-{Guid.NewGuid():N}.db");

    public TestDatabase()
    {
        using var context = CreateContext();
        context.Database.Migrate();
    }

    public CatalogDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<CatalogDbContext>()
            .UseSqlite($"Data Source={_path};Pooling=False")
            .AddInterceptors(new SqlitePragmaInterceptor())
            .Options;
        return new CatalogDbContext(options);
    }

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        foreach (var file in Directory.GetFiles(Path.GetTempPath(), Path.GetFileName(_path) + "*"))
        {
            File.Delete(file);
        }
    }
}
