using Auth.Domain.Emails;
using Auth.Domain.Tokens;
using Auth.Domain.Users;
using Auth.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Auth.IntegrationTests.Persistence;

public sealed class SchemaTests : IAsyncLifetime
{
    private static readonly DateTime Now = new(2026, 10, 10, 8, 30, 0, DateTimeKind.Utc);

    private readonly string _path = Path.Combine(Path.GetTempPath(), $"auth-schema-{Guid.NewGuid():N}.db");

    public async Task InitializeAsync()
    {
        await using var context = CreateContext();
        await context.Database.EnsureCreatedAsync();
    }

    public Task DisposeAsync()
    {
        SqliteConnection.ClearAllPools();
        foreach (var file in Directory.GetFiles(Path.GetTempPath(), Path.GetFileName(_path) + "*"))
        {
            File.Delete(file);
        }
        return Task.CompletedTask;
    }

    [Fact]
    public async Task Schema_ContainsTheFourTables()
    {
        await using var context = CreateContext();

        var tables = await QueryStringsAsync(context, "SELECT name FROM sqlite_master WHERE type = 'table'");

        Assert.Contains("users", tables);
        Assert.Contains("refresh_tokens", tables);
        Assert.Contains("reset_tokens", tables);
        Assert.Contains("email_outbox", tables);
    }

    [Fact]
    public async Task Users_EmailDifferingOnlyByCase_IsRejected()
    {
        await using var context = CreateContext();
        context.Users.Add(NewUser("user@example.com"));
        await context.SaveChangesAsync();

        context.Users.Add(NewUser("USER@Example.com"));

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    [Fact]
    public async Task Users_InvalidRole_IsRejectedByCheckConstraint()
    {
        await using var context = CreateContext();

        var insert = () => context.Database.ExecuteSqlRawAsync(
            "INSERT INTO users (id, email, name, password_hash, role, failed_attempts, created_at, updated_at) "
            + "VALUES ('u1', 'a@example.com', 'A', 'h', 'boss', 0, '2026-10-10T08:30:00Z', '2026-10-10T08:30:00Z')");

        await Assert.ThrowsAsync<SqliteException>(insert);
    }

    [Fact]
    public async Task DeletingUser_CascadesToTokensAndOutbox()
    {
        await using var context = CreateContext();
        var user = NewUser("user@example.com");
        context.Users.Add(user);
        context.RefreshTokens.Add(RefreshToken.Issue(user.Id, Guid.NewGuid(), "refresh-hash", Now));
        context.ResetTokens.Add(ResetToken.Issue(user.Id, "reset-hash", Now));
        context.EmailOutboxes.Add(EmailOutbox.Create(user.Id, user.Email, "Subject", "Body", Now));
        await context.SaveChangesAsync();

        await context.Database.ExecuteSqlRawAsync("DELETE FROM users");

        Assert.Equal(0, await CountAsync(context, "refresh_tokens"));
        Assert.Equal(0, await CountAsync(context, "reset_tokens"));
        Assert.Equal(0, await CountAsync(context, "email_outbox"));
    }

    [Fact]
    public async Task Connection_AppliesThePragmas()
    {
        await using var context = CreateContext();

        Assert.Equal("1", await ScalarAsync(context, "PRAGMA foreign_keys"));
        Assert.Equal("wal", await ScalarAsync(context, "PRAGMA journal_mode"));
        Assert.Equal("5000", await ScalarAsync(context, "PRAGMA busy_timeout"));
    }

    [Fact]
    public async Task Times_AreStoredAsIsoUtcTextAndReadBackAsUtc()
    {
        await using var writer = CreateContext();
        var user = NewUser("user@example.com");
        for (var i = 0; i < 5; i++)
        {
            user.RecordFailedLogin(Now);
        }
        writer.Users.Add(user);
        await writer.SaveChangesAsync();

        Assert.Equal("2026-10-10T08:30:00Z", await ScalarAsync(writer, "SELECT created_at FROM users"));
        Assert.Equal("2026-10-10T08:45:00Z", await ScalarAsync(writer, "SELECT locked_until FROM users"));
        await using var reader = CreateContext();
        var loaded = await reader.Users.SingleAsync();
        Assert.Equal(Now, loaded.CreatedAt);
        Assert.Equal(DateTimeKind.Utc, loaded.CreatedAt.Kind);
        Assert.Equal(Now.AddMinutes(15), loaded.LockedUntil);
    }

    [Fact]
    public async Task Enums_AreStoredAsDocumentedText()
    {
        await using var context = CreateContext();
        var user = NewUser("user@example.com");
        context.Users.Add(user);
        context.EmailOutboxes.Add(EmailOutbox.Create(user.Id, user.Email, "Subject", "Body", Now));
        await context.SaveChangesAsync();

        Assert.Equal("user", await ScalarAsync(context, "SELECT role FROM users"));
        Assert.Equal("Pending", await ScalarAsync(context, "SELECT status FROM email_outbox"));
    }

    [Fact]
    public async Task EmailOutbox_PendingIndexIsPartial()
    {
        await using var context = CreateContext();

        var sql = await ScalarAsync(context, "SELECT sql FROM sqlite_master WHERE name = 'ix_email_outbox_pending'");

        Assert.Contains("WHERE", sql);
        Assert.Contains("'Pending'", sql);
    }

    private AuthDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AuthDbContext>()
            .UseSqlite($"Data Source={_path};Pooling=False")
            .AddInterceptors(new SqlitePragmaInterceptor())
            .Options;
        return new AuthDbContext(options);
    }

    private static User NewUser(string email)
    {
        return User.Create(email, "Somchai", "hash", UserRole.User, Now);
    }

    private static async Task<List<string>> QueryStringsAsync(AuthDbContext context, string sql)
    {
        await context.Database.OpenConnectionAsync();
        await using var command = context.Database.GetDbConnection().CreateCommand();
        command.CommandText = sql;
        await using var reader = await command.ExecuteReaderAsync();
        var values = new List<string>();
        while (await reader.ReadAsync())
        {
            values.Add(reader.GetString(0));
        }
        return values;
    }

    private static async Task<string> ScalarAsync(AuthDbContext context, string sql)
    {
        await context.Database.OpenConnectionAsync();
        await using var command = context.Database.GetDbConnection().CreateCommand();
        command.CommandText = sql;
        var value = await command.ExecuteScalarAsync();
        return Convert.ToString(value) ?? string.Empty;
    }

    private static async Task<int> CountAsync(AuthDbContext context, string table)
    {
        return int.Parse(await ScalarAsync(context, $"SELECT COUNT(*) FROM {table}"));
    }
}
