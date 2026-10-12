using Catalog.Application.Events;
using Catalog.Application.Messaging;
using Catalog.Application.Reservations;
using Catalog.Domain.Messaging;
using Catalog.Domain.Reservations;
using Catalog.UnitTests.Fakes;

namespace Catalog.UnitTests.Reservations;

public class CommitReservationTests
{
    private readonly FakeInboxRepository _inbox = new();
    private readonly FakeReservationRepository _reservations = new();
    private readonly FakeUnitOfWork _unitOfWork = new();
    private readonly FakeTimeProvider _timeProvider = new();

    private DateTime Now => _timeProvider.GetUtcNow().UtcDateTime;

    private Task<EventOutcome> Run(OrderConfirmed message)
    {
        var useCase = new CommitReservation(_inbox, _reservations, _unitOfWork, _timeProvider);
        return useCase.ExecuteAsync(message, "corr-1", CancellationToken.None);
    }

    private static OrderConfirmed Confirmed(Guid orderId)
    {
        return new OrderConfirmed(Guid.NewGuid(), DateTime.UtcNow, 1, orderId);
    }

    private Reservation AddReserved(Guid orderId)
    {
        var reservation = Reservation.Reserve(orderId, [(Guid.NewGuid(), 2)], Now);
        _reservations.Items.Add(reservation);
        return reservation;
    }

    [Fact]
    public async Task Reserved_BecomesCommitted()
    {
        var orderId = Guid.NewGuid();
        var reservation = AddReserved(orderId);

        var outcome = await Run(Confirmed(orderId));

        Assert.Equal(EventOutcome.Applied, outcome);
        Assert.Equal(ReservationStatus.Committed, reservation.Status);
        Assert.Single(_inbox.Items);
        Assert.Equal(1, _unitOfWork.SaveCount);
    }

    [Fact]
    public async Task DuplicateEventId_IsSkipped()
    {
        var orderId = Guid.NewGuid();
        var reservation = AddReserved(orderId);
        var message = Confirmed(orderId);
        _inbox.Items.Add(InboxMessage.Create(message.EventId, Topics.OrderConfirmed, Now));

        var outcome = await Run(message);

        Assert.Equal(EventOutcome.Duplicate, outcome);
        Assert.Equal(ReservationStatus.Reserved, reservation.Status);
        Assert.Equal(0, _unitOfWork.SaveCount);
    }

    [Fact]
    public async Task NoReservation_IsIgnoredButStillRecordedInInbox()
    {
        var outcome = await Run(Confirmed(Guid.NewGuid()));

        Assert.Equal(EventOutcome.Ignored, outcome);
        Assert.Single(_inbox.Items);
        Assert.Equal(1, _unitOfWork.SaveCount);
    }

    [Fact]
    public async Task AlreadyCommitted_IsIgnored()
    {
        var orderId = Guid.NewGuid();
        var reservation = AddReserved(orderId);
        reservation.Commit(Now);

        var outcome = await Run(Confirmed(orderId));

        Assert.Equal(EventOutcome.Ignored, outcome);
        Assert.Equal(ReservationStatus.Committed, reservation.Status);
    }

    [Fact]
    public async Task AlreadyReleased_IsIgnoredAndStaysReleased()
    {
        var orderId = Guid.NewGuid();
        var reservation = Reservation.CreateTombstone(orderId, Now);
        _reservations.Items.Add(reservation);

        var outcome = await Run(Confirmed(orderId));

        Assert.Equal(EventOutcome.Ignored, outcome);
        Assert.Equal(ReservationStatus.Released, reservation.Status);
    }
}
