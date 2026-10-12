using Microsoft.EntityFrameworkCore;

namespace Catalog.IntegrationTests.Persistence;

public sealed class MigrationTests : IDisposable
{
    private readonly TestDatabase _database = new();

    public void Dispose()
    {
        _database.Dispose();
    }

    [Fact]
    public async Task Migrate_CreatesExactlyTheExpectedTables()
    {
        await using var context = _database.CreateContext();

        var tables = await context.Database
            .SqlQueryRaw<string>("SELECT name AS Value FROM sqlite_master WHERE type = 'table' AND name NOT LIKE 'sqlite_%'")
            .ToListAsync();

        string[] expected =
        [
            "__EFMigrationsHistory", "__EFMigrationsLock", "categories", "inbox", "outbox", "products", "reservation_items", "reservations"
        ];
        Assert.Equal(expected, tables.Order().ToArray());
    }

    [Fact]
    public async Task Categories_HaveNoDescriptionColumn()
    {
        await using var context = _database.CreateContext();

        var columns = await ColumnNamesAsync(context, "categories");

        Assert.Equal(["created_at", "deleted_at", "id", "is_deleted", "name", "updated_at"], columns);
    }

    [Fact]
    public async Task Products_HaveNoVersionColumn()
    {
        await using var context = _database.CreateContext();

        var columns = await ColumnNamesAsync(context, "products");

        Assert.DoesNotContain("version", columns);
        Assert.Contains("category_id", columns);
        Assert.Contains("price_minor", columns);
    }

    [Fact]
    public async Task Indexes_ExistWithPartialFilters()
    {
        await using var context = _database.CreateContext();

        var indexSql = await context.Database
            .SqlQueryRaw<string>("SELECT name || ' :: ' || sql AS Value FROM sqlite_master WHERE type = 'index' AND sql IS NOT NULL")
            .ToListAsync();

        var text = string.Join("\n", indexSql);
        Assert.Contains("ux_categories_name_active :: CREATE UNIQUE INDEX", text);
        Assert.Contains("WHERE is_deleted = 0", text);
        Assert.Contains("ix_products_list", text);
        Assert.Contains("ix_products_name", text);
        Assert.Contains("ix_outbox_pending", text);
        Assert.Contains("WHERE status = 'Pending'", text);
    }

    [Fact]
    public async Task Migrate_RunTwice_IsNoOp()
    {
        await using var context = _database.CreateContext();

        await context.Database.MigrateAsync();

        Assert.Empty(await context.Database.GetPendingMigrationsAsync());
        Assert.Single(await context.Database.GetAppliedMigrationsAsync());
    }

    [Fact]
    public async Task Model_HasNoPendingChanges()
    {
        await using var context = _database.CreateContext();

        Assert.False(context.Database.HasPendingModelChanges());
    }

    private static async Task<string[]> ColumnNamesAsync(DbContext context, string table)
    {
        var columns = await context.Database
            .SqlQuery<string>($"SELECT name AS Value FROM pragma_table_info({table})")
            .ToListAsync();
        return columns.Order().ToArray();
    }
}
