using Catalog.Application.Common;
using Catalog.Application.Events;
using Catalog.Application.Messaging;
using Catalog.Application.Stock;
using Catalog.Domain.Messaging;
using Catalog.Domain.Reservations;

namespace Catalog.Application.Reservations;

public sealed class ReleaseReservation(
    IInboxRepository inbox,
    IReservationRepository reservations,
    IStockRepository stock,
    ITransactionFactory transactions,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
{
    public async Task<EventOutcome> ExecuteAsync(
        OrderCancelled message,
        string correlationId,
        CancellationToken cancellationToken)
    {
        if (await inbox.ExistsAsync(message.EventId, cancellationToken))
        {
            return EventOutcome.Duplicate;
        }

        var now = timeProvider.GetUtcNow().UtcDateTime;
        var reservation = await reservations.FindAsync(message.OrderId, cancellationToken);
        if (reservation is null)
        {
            // Cancel arrived before order.created: leave a tombstone so the late order.created deducts nothing.
            await reservations.AddAsync(Reservation.CreateTombstone(message.OrderId, now), cancellationToken);
            await RecordAsync(message, now, cancellationToken);
            return EventOutcome.Applied;
        }

        await using var transaction = await transactions.BeginAsync(cancellationToken);
        var released = reservation.Release(now);
        if (released)
        {
            await RestoreStockAsync(reservation, now, cancellationToken);
        }

        await RecordAsync(message, now, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return released ? EventOutcome.Applied : EventOutcome.Ignored;
    }

    private async Task RestoreStockAsync(Reservation reservation, DateTime now, CancellationToken cancellationToken)
    {
        foreach (var item in reservation.Items)
        {
            await stock.RestoreAsync(item.ProductId, item.Quantity, now, cancellationToken);
        }
    }

    private async Task RecordAsync(OrderCancelled message, DateTime now, CancellationToken cancellationToken)
    {
        await inbox.AddAsync(InboxMessage.Create(message.EventId, Topics.OrderCancelled, now), cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
