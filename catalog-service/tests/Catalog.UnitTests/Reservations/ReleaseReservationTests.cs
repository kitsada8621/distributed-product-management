using Catalog.Application.Events;
using Catalog.Application.Messaging;
using Catalog.Application.Reservations;
using Catalog.Domain.Messaging;
using Catalog.Domain.Reservations;
using Catalog.UnitTests.Fakes;

namespace Catalog.UnitTests.Reservations;

public class ReleaseReservationTests
{
    private readonly FakeInboxRepository _inbox = new();
    private readonly FakeReservationRepository _reservations = new();
    private readonly FakeStockRepository _stock = new();
    private readonly FakeOutboxRepository _outbox = new();
    private readonly FakeUnitOfWork _unitOfWork = new();
    private readonly FakeTimeProvider _timeProvider = new();
    private readonly FakeTransactionFactory _transactions;

    public ReleaseReservationTests()
    {
        _transactions = new FakeTransactionFactory(_stock);
    }

    private DateTime Now => _timeProvider.GetUtcNow().UtcDateTime;

    private Task<EventOutcome> Release(OrderCancelled message)
    {
        var useCase = new ReleaseReservation(_inbox, _reservations, _stock, _transactions, _unitOfWork, _timeProvider);
        return useCase.ExecuteAsync(message, "corr-1", CancellationToken.None);
    }

    private static OrderCancelled Cancelled(Guid orderId)
    {
        return new OrderCancelled(Guid.NewGuid(), DateTime.UtcNow, 1, orderId, "USER_CANCELLED");
    }

    private Task<EventOutcome> Reserve(OrderCreated message)
    {
        var useCase = new ReserveStock(
            _inbox, _reservations, _stock, _outbox, _transactions, _unitOfWork, _timeProvider);
        return useCase.ExecuteAsync(message, "corr-1", CancellationToken.None);
    }

    private Guid AddProduct(int stock, bool deleted = false)
    {
        var id = Guid.NewGuid();
        _stock.AddProduct(id, "Tea", 1000, stock, deleted);
        return id;
    }

    [Fact]
    public async Task Reserved_BecomesReleasedAndEveryItemIsAddedBack()
    {
        var tea = AddProduct(3);
        var gone = AddProduct(1, deleted: true);
        var orderId = Guid.NewGuid();
        var reservation = Reservation.Reserve(orderId, [(tea, 2), (gone, 4)], Now);
        _reservations.Items.Add(reservation);

        var outcome = await Release(Cancelled(orderId));

        Assert.Equal(EventOutcome.Applied, outcome);
        Assert.Equal(ReservationStatus.Released, reservation.Status);
        Assert.Equal(5, _stock.StockOf(tea));
        Assert.Equal(5, _stock.StockOf(gone));
        Assert.Single(_inbox.Items);
        Assert.Equal(1, _transactions.Committed);
    }

    [Fact]
    public async Task NoReservation_CreatesAReleasedTombstoneWithNoItems()
    {
        var orderId = Guid.NewGuid();

        var outcome = await Release(Cancelled(orderId));

        Assert.Equal(EventOutcome.Applied, outcome);
        var tombstone = Assert.Single(_reservations.Items);
        Assert.Equal(orderId, tombstone.OrderId);
        Assert.Equal(ReservationStatus.Released, tombstone.Status);
        Assert.Empty(tombstone.Items);
        Assert.Single(_inbox.Items);
    }

    [Fact]
    public async Task OrderCreatedAfterTombstone_DeductsNothing()
    {
        var tea = AddProduct(5);
        var orderId = Guid.NewGuid();
        await Release(Cancelled(orderId));
        var created = new OrderCreated(Guid.NewGuid(), DateTime.UtcNow, 1, orderId, "u1", [new OrderCreatedItem(tea, 2)]);

        var outcome = await Reserve(created);

        Assert.Equal(EventOutcome.Ignored, outcome);
        Assert.Equal(5, _stock.StockOf(tea));
        Assert.Empty(_outbox.Items);
    }

    [Fact]
    public async Task SecondCancelWithNewEventId_DoesNotAddStockTwice()
    {
        var tea = AddProduct(3);
        var orderId = Guid.NewGuid();
        _reservations.Items.Add(Reservation.Reserve(orderId, [(tea, 2)], Now));
        await Release(Cancelled(orderId));

        var outcome = await Release(Cancelled(orderId));

        Assert.Equal(EventOutcome.Ignored, outcome);
        Assert.Equal(5, _stock.StockOf(tea));
    }

    [Fact]
    public async Task SameEventIdTwice_IsDuplicate()
    {
        var tea = AddProduct(3);
        var orderId = Guid.NewGuid();
        _reservations.Items.Add(Reservation.Reserve(orderId, [(tea, 2)], Now));
        var message = Cancelled(orderId);
        await Release(message);

        var outcome = await Release(message);

        Assert.Equal(EventOutcome.Duplicate, outcome);
        Assert.Equal(5, _stock.StockOf(tea));
    }

    [Fact]
    public async Task Committed_IsIgnoredAndStockIsUntouched()
    {
        var tea = AddProduct(3);
        var orderId = Guid.NewGuid();
        var reservation = Reservation.Reserve(orderId, [(tea, 2)], Now);
        reservation.Commit(Now);
        _reservations.Items.Add(reservation);

        var outcome = await Release(Cancelled(orderId));

        Assert.Equal(EventOutcome.Ignored, outcome);
        Assert.Equal(ReservationStatus.Committed, reservation.Status);
        Assert.Equal(3, _stock.StockOf(tea));
        Assert.Single(_inbox.Items);
    }
}
