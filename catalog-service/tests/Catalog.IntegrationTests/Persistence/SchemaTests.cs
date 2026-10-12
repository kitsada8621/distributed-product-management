using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Catalog.IntegrationTests.Persistence;

// Raw SQL on the migrated schema, so the database itself (not the EF model) is what rejects bad rows.
public sealed class SchemaTests : IDisposable
{
    private const string At = "'2026-10-10T08:30:00Z'";

    private readonly TestDatabase _database = new();

    public void Dispose()
    {
        _database.Dispose();
    }

    private async Task ExecuteAsync(string sql)
    {
        await using var context = _database.CreateContext();
        await context.Database.ExecuteSqlRawAsync(sql);
    }

    private Task InsertCategoryAsync(string id, string name, int isDeleted = 0)
    {
        return ExecuteAsync(
            "INSERT INTO categories (id, name, is_deleted, created_at, updated_at) "
            + $"VALUES ('{id}', '{name}', {isDeleted}, {At}, {At})");
    }

    private Task InsertProductAsync(string id, string? categoryId, long priceMinor, int stock, int isDeleted = 0)
    {
        var category = categoryId is null ? "NULL" : $"'{categoryId}'";
        return ExecuteAsync(
            "INSERT INTO products (id, category_id, name, description, price_minor, stock, is_deleted, created_at, updated_at) "
            + $"VALUES ('{id}', {category}, 'Tea', '', {priceMinor}, {stock}, {isDeleted}, {At}, {At})");
    }

    [Fact]
    public async Task Product_PriceZero_IsRejected()
    {
        await Assert.ThrowsAsync<SqliteException>(() => InsertProductAsync("p1", null, 0, 1));
    }

    [Fact]
    public async Task Product_NegativeStock_IsRejected()
    {
        await Assert.ThrowsAsync<SqliteException>(() => InsertProductAsync("p1", null, 100, -1));
    }

    [Fact]
    public async Task Product_IsDeletedTwo_IsRejected()
    {
        await Assert.ThrowsAsync<SqliteException>(() => InsertProductAsync("p1", null, 100, 1, isDeleted: 2));
    }

    [Fact]
    public async Task Category_IsDeletedTwo_IsRejected()
    {
        await Assert.ThrowsAsync<SqliteException>(() => InsertCategoryAsync("c1", "Drinks", isDeleted: 2));
    }

    [Fact]
    public async Task Product_UnknownCategory_IsRejectedByForeignKey()
    {
        await Assert.ThrowsAsync<SqliteException>(() => InsertProductAsync("p1", "missing", 100, 1));
    }

    [Fact]
    public async Task Product_NullCategory_IsAccepted()
    {
        await InsertProductAsync("p1", null, 100, 1);
    }

    [Fact]
    public async Task Category_DuplicateActiveNameDifferingByCase_IsRejected()
    {
        await InsertCategoryAsync("c1", "Drinks");

        await Assert.ThrowsAsync<SqliteException>(() => InsertCategoryAsync("c2", "DRINKS"));
    }

    [Fact]
    public async Task Category_NameOfDeletedCategory_CanBeReused()
    {
        await InsertCategoryAsync("c1", "Drinks", isDeleted: 1);

        await InsertCategoryAsync("c2", "Drinks");
    }

    [Fact]
    public async Task Category_HardDeleteWithProducts_IsRestrictedByForeignKey()
    {
        await InsertCategoryAsync("c1", "Drinks");
        await InsertProductAsync("p1", "c1", 100, 1);

        await Assert.ThrowsAsync<SqliteException>(() => ExecuteAsync("DELETE FROM categories WHERE id = 'c1'"));
    }

    [Fact]
    public async Task Reservation_UnknownStatus_IsRejected()
    {
        var insert = () => ExecuteAsync(
            $"INSERT INTO reservations (order_id, status, created_at, updated_at) VALUES ('o1', 'X', {At}, {At})");

        await Assert.ThrowsAsync<SqliteException>(insert);
    }

    [Fact]
    public async Task ReservationItem_QuantityZero_IsRejected()
    {
        await ExecuteAsync(
            $"INSERT INTO reservations (order_id, status, created_at, updated_at) VALUES ('o1', 'Reserved', {At}, {At})");

        var insert = () => ExecuteAsync(
            "INSERT INTO reservation_items (reservation_id, product_id, quantity) VALUES ('o1', 'p1', 0)");

        await Assert.ThrowsAsync<SqliteException>(insert);
    }

    [Fact]
    public async Task ReservationItem_UnknownReservation_IsRejected()
    {
        var insert = () => ExecuteAsync(
            "INSERT INTO reservation_items (reservation_id, product_id, quantity) VALUES ('missing', 'p1', 1)");

        await Assert.ThrowsAsync<SqliteException>(insert);
    }

    [Fact]
    public async Task Outbox_UnknownStatus_IsRejected()
    {
        var insert = () => ExecuteAsync(
            "INSERT INTO outbox (id, topic, message_key, payload, correlation_id, status, attempts, next_attempt_at, created_at) "
            + $"VALUES ('e1', 'stock.reserved', 'o1', 'payload', 'c', 'X', 0, {At}, {At})");

        await Assert.ThrowsAsync<SqliteException>(insert);
    }

    [Fact]
    public async Task Outbox_StatusDefaultsToPending()
    {
        await ExecuteAsync(
            "INSERT INTO outbox (id, topic, message_key, payload, correlation_id, next_attempt_at, created_at) "
            + $"VALUES ('e1', 'stock.reserved', 'o1', 'payload', 'c', {At}, {At})");

        await using var context = _database.CreateContext();
        var status = await context.Database
            .SqlQueryRaw<string>("SELECT status AS Value FROM outbox WHERE id = 'e1'")
            .SingleAsync();
        Assert.Equal("Pending", status);
    }

    [Fact]
    public async Task Inbox_DuplicateEventId_IsRejected()
    {
        var insert = () => ExecuteAsync($"INSERT INTO inbox (event_id, topic, processed_at) VALUES ('e1', 'order.created', {At})");
        await insert();

        await Assert.ThrowsAsync<SqliteException>(insert);
    }
}
