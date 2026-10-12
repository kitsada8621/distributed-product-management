using Auth.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Auth.IntegrationTests.Persistence;

public sealed class TestDatabase : IDisposable
{
    private readonly string _path = Path.Combine(Path.GetTempPath(), $"auth-test-{Guid.NewGuid():N}.db");

    public TestDatabase()
    {
        using var context = CreateContext();
        context.Database.Migrate();
    }

    public AuthDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AuthDbContext>()
            .UseSqlite($"Data Source={_path};Pooling=False")
            .AddInterceptors(new SqlitePragmaInterceptor())
            .Options;
        return new AuthDbContext(options);
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
