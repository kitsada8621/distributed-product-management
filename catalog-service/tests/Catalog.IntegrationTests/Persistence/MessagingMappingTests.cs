using Catalog.Domain.Messaging;
using Catalog.Domain.Reservations;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Catalog.IntegrationTests.Persistence;

public sealed class MessagingMappingTests : IDisposable
{
    private static readonly DateTime Now = new(2026, 10, 10, 8, 30, 0, DateTimeKind.Utc);

    private readonly TestDatabase _database = new();

    public void Dispose()
    {
        _database.Dispose();
    }

    [Fact]
    public async Task Reservation_WithTwoItems_RoundTrips()
    {
        var orderId = Guid.NewGuid();
        var productA = Guid.NewGuid();
        var productB = Guid.NewGuid();
        await using (var context = _database.CreateContext())
        {
            context.Reservations.Add(Reservation.Reserve(orderId, [(productA, 2), (productB, 3)], Now));
            await context.SaveChangesAsync();
        }

        await using var readContext = _database.CreateContext();
        var saved = await readContext.Reservations.Include(r => r.Items).SingleAsync();

        Assert.Equal(orderId, saved.OrderId);
        Assert.Equal(ReservationStatus.Reserved, saved.Status);
        Assert.Equal(2, saved.Items.Count);
        Assert.Contains(saved.Items, item => item.ProductId == productA && item.Quantity == 2);
        Assert.Contains(saved.Items, item => item.ProductId == productB && item.Quantity == 3);
    }

    [Fact]
    public async Task DeletingReservation_DeletesItsItems()
    {
        await using var context = _database.CreateContext();
        var reservation = Reservation.Reserve(Guid.NewGuid(), [(Guid.NewGuid(), 1)], Now);
        context.Reservations.Add(reservation);
        await context.SaveChangesAsync();

        context.Reservations.Remove(reservation);
        await context.SaveChangesAsync();

        Assert.Equal(0, await context.ReservationItems.CountAsync());
    }

    [Fact]
    public async Task ReservationItem_QuantityZero_IsRejectedByCheckConstraint()
    {
        await using var context = _database.CreateContext();
        context.Reservations.Add(Reservation.Reserve(Guid.NewGuid(), [(Guid.NewGuid(), 0)], Now));

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    [Fact]
    public async Task Reservation_UnknownStatusText_IsRejectedByCheckConstraint()
    {
        await using var context = _database.CreateContext();

        var insert = () => context.Database.ExecuteSqlRawAsync(
            "INSERT INTO reservations (order_id, status, created_at, updated_at) "
            + "VALUES ('o-1', 'Unknown', '2026-10-10T08:30:00Z', '2026-10-10T08:30:00Z')");

        await Assert.ThrowsAsync<SqliteException>(insert);
    }

    [Fact]
    public async Task ReservationItem_UnknownReservation_IsRejectedByForeignKey()
    {
        await using var context = _database.CreateContext();
        context.ReservationItems.Add(new ReservationItem(Guid.NewGuid(), Guid.NewGuid(), 1));

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    [Fact]
    public async Task ReservationItem_ProductWithoutRow_IsAccepted()
    {
        await using var context = _database.CreateContext();
        context.Reservations.Add(Reservation.Reserve(Guid.NewGuid(), [(Guid.NewGuid(), 2)], Now));

        await context.SaveChangesAsync();

        Assert.Equal(1, await context.ReservationItems.CountAsync());
    }

    [Fact]
    public async Task OutboxMessage_RoundTripsAsPending()
    {
        var id = Guid.NewGuid();
        await using (var context = _database.CreateContext())
        {
            context.OutboxMessages.Add(
                OutboxMessage.Create(id, "stock.reserved", "order-1", "{\"a\":1}", "corr-1", Now));
            await context.SaveChangesAsync();
        }

        await using var readContext = _database.CreateContext();
        var saved = await readContext.OutboxMessages.SingleAsync();

        Assert.Equal(id, saved.Id);
        Assert.Equal("stock.reserved", saved.Topic);
        Assert.Equal("order-1", saved.MessageKey);
        Assert.Equal("{\"a\":1}", saved.Payload);
        Assert.Equal("corr-1", saved.CorrelationId);
        Assert.Equal(OutboxStatus.Pending, saved.Status);
        Assert.Equal(0, saved.Attempts);
        Assert.Equal(Now, saved.NextAttemptAt);
        Assert.Null(saved.SentAt);
    }

    [Fact]
    public async Task InboxMessage_SameEventIdTwice_IsRejectedByPrimaryKey()
    {
        var eventId = Guid.NewGuid();
        await using var context = _database.CreateContext();
        context.InboxMessages.Add(InboxMessage.Create(eventId, "order.created", Now));
        await context.SaveChangesAsync();

        await using var secondContext = _database.CreateContext();
        secondContext.InboxMessages.Add(InboxMessage.Create(eventId, "order.created", Now));

        await Assert.ThrowsAsync<DbUpdateException>(() => secondContext.SaveChangesAsync());
    }
}
