using Catalog.Domain.Messaging;
using Catalog.Domain.Reservations;
using Catalog.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Catalog.IntegrationTests.Persistence;

public sealed class MessagingRepositoryTests : IDisposable
{
    private static readonly DateTime Now = new(2026, 10, 10, 8, 30, 0, DateTimeKind.Utc);

    private readonly TestDatabase _database = new();

    public void Dispose()
    {
        _database.Dispose();
    }

    private static OutboxMessage Message(DateTime due, string key = "order-1")
    {
        return OutboxMessage.Create(Guid.NewGuid(), "stock.reserved", key, "{}", "corr", due);
    }

    private async Task SeedAsync(params OutboxMessage[] messages)
    {
        await using var context = _database.CreateContext();
        context.OutboxMessages.AddRange(messages);
        await context.SaveChangesAsync();
    }

    [Fact]
    public async Task ListDue_ReturnsOnlyDuePendingRowsOldestFirst()
    {
        var older = Message(Now.AddSeconds(-30), "older");
        var newer = Message(Now.AddSeconds(-5), "newer");
        var notDue = Message(Now.AddSeconds(10), "not-due");
        var sent = Message(Now.AddSeconds(-60), "sent");
        sent.MarkSent(Now);
        await SeedAsync(newer, older, notDue, sent);
        await using var context = _database.CreateContext();

        var due = await new OutboxRepository(context).ListDueAsync(Now, 10, CancellationToken.None);

        Assert.Equal(["older", "newer"], due.Select(m => m.MessageKey).ToArray());
    }

    [Fact]
    public async Task ListDue_RespectsTheLimit()
    {
        await SeedAsync(Message(Now.AddSeconds(-3)), Message(Now.AddSeconds(-2)), Message(Now.AddSeconds(-1)));
        await using var context = _database.CreateContext();

        var due = await new OutboxRepository(context).ListDueAsync(Now, 2, CancellationToken.None);

        Assert.Equal(2, due.Count);
    }

    [Fact]
    public async Task ListDue_ReturnsTrackedRowsSoUpdatesAreSaved()
    {
        await SeedAsync(Message(Now.AddSeconds(-1)));
        await using (var context = _database.CreateContext())
        {
            var due = await new OutboxRepository(context).ListDueAsync(Now, 10, CancellationToken.None);
            due[0].MarkSent(Now);
            await context.SaveChangesAsync();
        }

        await using var readContext = _database.CreateContext();
        Assert.Equal(OutboxStatus.Sent, (await readContext.OutboxMessages.SingleAsync()).Status);
    }

    [Fact]
    public async Task Inbox_ExistsIsFalseThenTrueAfterAddAndSave()
    {
        var eventId = Guid.NewGuid();
        await using (var context = _database.CreateContext())
        {
            var inbox = new InboxRepository(context);
            Assert.False(await inbox.ExistsAsync(eventId, CancellationToken.None));
            await inbox.AddAsync(InboxMessage.Create(eventId, "order.created", Now), CancellationToken.None);
            await context.SaveChangesAsync();
        }

        await using var readContext = _database.CreateContext();
        Assert.True(await new InboxRepository(readContext).ExistsAsync(eventId, CancellationToken.None));
    }

    [Fact]
    public async Task Reservation_IsFoundWithItsItems()
    {
        var orderId = Guid.NewGuid();
        await using (var context = _database.CreateContext())
        {
            var reservation = Reservation.Reserve(orderId, [(Guid.NewGuid(), 2), (Guid.NewGuid(), 3)], Now);
            await new ReservationRepository(context).AddAsync(reservation, CancellationToken.None);
            await context.SaveChangesAsync();
        }

        await using var readContext = _database.CreateContext();
        var found = await new ReservationRepository(readContext).FindAsync(orderId, CancellationToken.None);

        Assert.Equal(2, found!.Items.Count);
        Assert.Null(await new ReservationRepository(readContext).FindAsync(Guid.NewGuid(), CancellationToken.None));
    }

    [Fact]
    public async Task Reservation_SecondWithSameOrderId_FailsOnSave()
    {
        var orderId = Guid.NewGuid();
        await using (var context = _database.CreateContext())
        {
            context.Reservations.Add(Reservation.CreateTombstone(orderId, Now));
            await context.SaveChangesAsync();
        }

        await using var secondContext = _database.CreateContext();
        secondContext.Reservations.Add(Reservation.CreateTombstone(orderId, Now));

        await Assert.ThrowsAsync<DbUpdateException>(() => secondContext.SaveChangesAsync());
    }

    [Fact]
    public async Task Transaction_RolledBack_LeavesTheRowAbsent()
    {
        var eventId = Guid.NewGuid();
        await using (var context = _database.CreateContext())
        {
            await using var transaction = await context.BeginAsync(CancellationToken.None);
            context.InboxMessages.Add(InboxMessage.Create(eventId, "order.created", Now));
            await context.SaveChangesAsync();
            await transaction.RollbackAsync(CancellationToken.None);
        }

        await using var readContext = _database.CreateContext();
        Assert.False(await readContext.InboxMessages.AnyAsync(m => m.EventId == eventId));
    }

    [Fact]
    public async Task Transaction_Committed_PersistsTheRow()
    {
        var eventId = Guid.NewGuid();
        await using (var context = _database.CreateContext())
        {
            await using var transaction = await context.BeginAsync(CancellationToken.None);
            context.InboxMessages.Add(InboxMessage.Create(eventId, "order.created", Now));
            await context.SaveChangesAsync();
            await transaction.CommitAsync(CancellationToken.None);
        }

        await using var readContext = _database.CreateContext();
        Assert.True(await readContext.InboxMessages.AnyAsync(m => m.EventId == eventId));
    }

    [Fact]
    public async Task Transaction_DisposedWithoutCommit_RollsBack()
    {
        var eventId = Guid.NewGuid();
        await using (var context = _database.CreateContext())
        {
            await using (var transaction = await context.BeginAsync(CancellationToken.None))
            {
                context.InboxMessages.Add(InboxMessage.Create(eventId, "order.created", Now));
                await context.SaveChangesAsync();
            }
        }

        await using var readContext = _database.CreateContext();
        Assert.False(await readContext.InboxMessages.AnyAsync(m => m.EventId == eventId));
    }
}
