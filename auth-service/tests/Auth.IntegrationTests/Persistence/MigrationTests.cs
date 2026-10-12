using Auth.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Auth.IntegrationTests.Persistence;

public sealed class MigrationTests : IDisposable
{
    private readonly string _path = Path.Combine(Path.GetTempPath(), $"auth-migration-{Guid.NewGuid():N}.db");

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        foreach (var file in Directory.GetFiles(Path.GetTempPath(), Path.GetFileName(_path) + "*"))
        {
            File.Delete(file);
        }
    }

    [Fact]
    public async Task Migrate_CreatesTablesIndexesAndHistory()
    {
        await using var context = CreateContext();

        await context.Database.MigrateAsync();

        var names = await QueryNamesAsync(context, "SELECT name FROM sqlite_master");
        string[] expected =
        [
            "users", "refresh_tokens", "reset_tokens", "email_outbox", "__EFMigrationsHistory",
            "ux_users_email",
            "ix_refresh_tokens_user", "ix_refresh_tokens_family", "ux_refresh_tokens_token_hash",
            "ix_reset_tokens_user", "ux_reset_tokens_token_hash",
            "ix_email_outbox_pending"
        ];
        foreach (var name in expected)
        {
            Assert.Contains(name, names);
        }
    }

    [Fact]
    public async Task Migrate_RunTwice_IsNoOp()
    {
        await using var context = CreateContext();
        await context.Database.MigrateAsync();

        await context.Database.MigrateAsync();

        Assert.Empty(await context.Database.GetPendingMigrationsAsync());
        Assert.Single(await context.Database.GetAppliedMigrationsAsync());
    }

    [Fact]
    public async Task Model_HasNoPendingChanges()
    {
        await using var context = CreateContext();

        Assert.False(context.Database.HasPendingModelChanges());
    }

    private AuthDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AuthDbContext>()
            .UseSqlite($"Data Source={_path};Pooling=False")
            .AddInterceptors(new SqlitePragmaInterceptor())
            .Options;
        return new AuthDbContext(options);
    }

    private static async Task<List<string>> QueryNamesAsync(AuthDbContext context, string sql)
    {
        await context.Database.OpenConnectionAsync();
        await using var command = context.Database.GetDbConnection().CreateCommand();
        command.CommandText = sql;
        await using var reader = await command.ExecuteReaderAsync();
        var names = new List<string>();
        while (await reader.ReadAsync())
        {
            names.Add(reader.GetString(0));
        }
        return names;
    }
}
