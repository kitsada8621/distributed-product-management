using Catalog.Application.Common;
using Catalog.Application.Events;
using Catalog.Application.Messaging;
using Catalog.Domain.Messaging;

namespace Catalog.Application.Reservations;

public sealed class CommitReservation(
    IInboxRepository inbox,
    IReservationRepository reservations,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
{
    public async Task<EventOutcome> ExecuteAsync(
        OrderConfirmed message,
        string correlationId,
        CancellationToken cancellationToken)
    {
        if (await inbox.ExistsAsync(message.EventId, cancellationToken))
        {
            return EventOutcome.Duplicate;
        }

        var now = timeProvider.GetUtcNow().UtcDateTime;
        var reservation = await reservations.FindAsync(message.OrderId, cancellationToken);
        var committed = reservation is not null && reservation.Commit(now);

        await inbox.AddAsync(InboxMessage.Create(message.EventId, Topics.OrderConfirmed, now), cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return committed ? EventOutcome.Applied : EventOutcome.Ignored;
    }
}
