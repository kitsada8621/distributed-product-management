using Catalog.Application.Common;
using Catalog.Application.Events;
using Catalog.Application.Messaging;
using Catalog.Application.Products;
using Catalog.Application.Stock;
using Catalog.Domain.Messaging;
using Catalog.Domain.Reservations;

namespace Catalog.Application.Reservations;

public sealed class ReserveStock(
    IInboxRepository inbox,
    IReservationRepository reservations,
    IStockRepository stock,
    IOutboxRepository outbox,
    ITransactionFactory transactions,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
{
    private const int EventVersion = 1;

    public async Task<EventOutcome> ExecuteAsync(
        OrderCreated message,
        string correlationId,
        CancellationToken cancellationToken)
    {
        if (await inbox.ExistsAsync(message.EventId, cancellationToken))
        {
            return EventOutcome.Duplicate;
        }

        var now = timeProvider.GetUtcNow().UtcDateTime;
        if (await reservations.FindAsync(message.OrderId, cancellationToken) is not null)
        {
            // A Released tombstone (cancel arrived first) or an earlier reservation: reply with nothing.
            await inbox.AddAsync(InboxMessage.Create(message.EventId, Topics.OrderCreated, now), cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return EventOutcome.Ignored;
        }

        await using var transaction = await transactions.BeginAsync(cancellationToken);
        var failedItems = await DeductAllAsync(message, now, cancellationToken);

        if (failedItems.Count > 0)
        {
            await transaction.RollbackAsync(cancellationToken);
            await RejectAsync(message, failedItems, correlationId, now, cancellationToken);
            return EventOutcome.Applied;
        }

        await ReserveAsync(message, correlationId, now, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return EventOutcome.Applied;
    }

    // Tries every item so the rejection lists all failures, not just the first.
    private async Task<List<StockFailedItem>> DeductAllAsync(
        OrderCreated message,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var failedItems = new List<StockFailedItem>();
        foreach (var item in message.Items)
        {
            if (await stock.TryDeductAsync(item.ProductId, item.Quantity, now, cancellationToken))
            {
                continue;
            }

            var available = await stock.GetActiveStockAsync(item.ProductId, cancellationToken);
            failedItems.Add(available is null
                ? new StockFailedItem(item.ProductId, StockFailureReasons.ProductNotFound, item.Quantity, 0)
                : new StockFailedItem(item.ProductId, StockFailureReasons.InsufficientStock, item.Quantity, available.Value));
        }
        return failedItems;
    }

    private async Task RejectAsync(
        OrderCreated message,
        List<StockFailedItem> failedItems,
        string correlationId,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var eventId = Guid.NewGuid();
        var rejected = new StockRejected(eventId, now, EventVersion, message.OrderId, failedItems);

        await inbox.AddAsync(InboxMessage.Create(message.EventId, Topics.OrderCreated, now), cancellationToken);
        await QueueAsync(Topics.StockRejected, eventId, message.OrderId, EventJson.Serialize(rejected), correlationId, now, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task ReserveAsync(
        OrderCreated message,
        string correlationId,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var productIds = message.Items.Select(item => item.ProductId).ToList();
        var snapshots = await stock.GetSnapshotsAsync(productIds, cancellationToken);

        var reservedItems = new List<StockReservedItem>();
        foreach (var item in message.Items)
        {
            var snapshot = snapshots.Single(s => s.Id == item.ProductId);
            var unitPrice = PriceConverter.ToDecimal(snapshot.PriceMinor);
            reservedItems.Add(new StockReservedItem(item.ProductId, snapshot.Name, unitPrice, item.Quantity));
        }

        var eventId = Guid.NewGuid();
        var reserved = new StockReserved(eventId, now, EventVersion, message.OrderId, reservedItems);
        var lines = message.Items.Select(item => (item.ProductId, item.Quantity));

        await reservations.AddAsync(Reservation.Reserve(message.OrderId, lines, now), cancellationToken);
        await inbox.AddAsync(InboxMessage.Create(message.EventId, Topics.OrderCreated, now), cancellationToken);
        await QueueAsync(Topics.StockReserved, eventId, message.OrderId, EventJson.Serialize(reserved), correlationId, now, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private Task QueueAsync(
        string topic,
        Guid eventId,
        Guid orderId,
        string payload,
        string correlationId,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var outboxMessage = OutboxMessage.Create(eventId, topic, orderId.ToString(), payload, correlationId, now);
        return outbox.AddAsync(outboxMessage, cancellationToken);
    }
}
