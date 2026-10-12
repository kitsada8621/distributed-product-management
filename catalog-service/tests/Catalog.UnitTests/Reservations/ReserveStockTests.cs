using Catalog.Application.Events;
using Catalog.Application.Messaging;
using Catalog.Application.Reservations;
using Catalog.Domain.Messaging;
using Catalog.Domain.Reservations;
using Catalog.UnitTests.Fakes;

namespace Catalog.UnitTests.Reservations;

public class ReserveStockTests
{
    private readonly FakeInboxRepository _inbox = new();
    private readonly FakeReservationRepository _reservations = new();
    private readonly FakeStockRepository _stock = new();
    private readonly FakeOutboxRepository _outbox = new();
    private readonly FakeUnitOfWork _unitOfWork = new();
    private readonly FakeTimeProvider _timeProvider = new();
    private readonly FakeTransactionFactory _transactions;

    public ReserveStockTests()
    {
        _transactions = new FakeTransactionFactory(_stock);
    }

    private Task<EventOutcome> Run(OrderCreated message, string correlationId = "corr-1")
    {
        var useCase = new ReserveStock(
            _inbox, _reservations, _stock, _outbox, _transactions, _unitOfWork, _timeProvider);
        return useCase.ExecuteAsync(message, correlationId, CancellationToken.None);
    }

    private static OrderCreated Order(Guid orderId, params (Guid ProductId, int Quantity)[] items)
    {
        var lines = items.Select(item => new OrderCreatedItem(item.ProductId, item.Quantity)).ToList();
        return new OrderCreated(Guid.NewGuid(), DateTime.UtcNow, 1, orderId, "u1", lines);
    }

    private Guid AddProduct(string name, int stock, long priceMinor = 129000, bool deleted = false)
    {
        var id = Guid.NewGuid();
        _stock.AddProduct(id, name, priceMinor, stock, deleted);
        return id;
    }

    [Fact]
    public async Task DuplicateEventId_IsSkippedAndChangesNothing()
    {
        var product = AddProduct("Tea", 5);
        var message = Order(Guid.NewGuid(), (product, 2));
        _inbox.Items.Add(InboxMessage.Create(message.EventId, Topics.OrderCreated, DateTime.UtcNow));

        var outcome = await Run(message);

        Assert.Equal(EventOutcome.Duplicate, outcome);
        Assert.Equal(5, _stock.StockOf(product));
        Assert.Empty(_outbox.Items);
        Assert.Empty(_reservations.Items);
        Assert.Equal(0, _unitOfWork.SaveCount);
    }

    [Fact]
    public async Task ExistingReleasedReservation_DeductsNothingAndSendsNothing()
    {
        var product = AddProduct("Tea", 5);
        var orderId = Guid.NewGuid();
        _reservations.Items.Add(Reservation.CreateTombstone(orderId, DateTime.UtcNow));
        var message = Order(orderId, (product, 2));

        var outcome = await Run(message);

        Assert.Equal(EventOutcome.Ignored, outcome);
        Assert.Equal(5, _stock.StockOf(product));
        Assert.Empty(_outbox.Items);
        Assert.Single(_inbox.Items);
        Assert.Equal(1, _unitOfWork.SaveCount);
    }

    [Fact]
    public async Task Success_DeductsStockReservesAndQueuesStockReserved()
    {
        var tea = AddProduct("Tea", 5, priceMinor: 129000);
        var coffee = AddProduct("Coffee", 10, priceMinor: 5050);
        var orderId = Guid.NewGuid();
        var message = Order(orderId, (tea, 2), (coffee, 3));

        var outcome = await Run(message, "corr-77");

        Assert.Equal(EventOutcome.Applied, outcome);
        Assert.Equal(3, _stock.StockOf(tea));
        Assert.Equal(7, _stock.StockOf(coffee));
        var reservation = Assert.Single(_reservations.Items);
        Assert.Equal(ReservationStatus.Reserved, reservation.Status);
        Assert.Equal(2, reservation.Items.Count);
        Assert.Single(_inbox.Items);
        Assert.Equal(1, _transactions.Committed);

        var queued = Assert.Single(_outbox.Items);
        Assert.Equal(Topics.StockReserved, queued.Topic);
        Assert.Equal(orderId.ToString(), queued.MessageKey);
        Assert.Equal("corr-77", queued.CorrelationId);
        Assert.Equal(OutboxStatus.Pending, queued.Status);
        var payload = EventJson.Deserialize<StockReserved>(queued.Payload);
        Assert.Equal(queued.Id, payload.EventId);
        Assert.Equal(orderId, payload.OrderId);
        var teaLine = payload.Items.Single(item => item.ProductId == tea);
        Assert.Equal("Tea", teaLine.Name);
        Assert.Equal(1290.00m, teaLine.UnitPrice);
        Assert.Equal(2, teaLine.Quantity);
    }

    [Fact]
    public async Task OneItemShort_DeductsNothingAndQueuesStockRejected()
    {
        var plenty = AddProduct("Plenty", 10);
        var scarce = AddProduct("Scarce", 1);
        var orderId = Guid.NewGuid();

        var outcome = await Run(Order(orderId, (plenty, 4), (scarce, 3)));

        Assert.Equal(EventOutcome.Applied, outcome);
        Assert.Equal(10, _stock.StockOf(plenty));
        Assert.Equal(1, _stock.StockOf(scarce));
        Assert.Empty(_reservations.Items);
        Assert.Single(_inbox.Items);
        var queued = Assert.Single(_outbox.Items);
        Assert.Equal(Topics.StockRejected, queued.Topic);
        var failed = Assert.Single(EventJson.Deserialize<StockRejected>(queued.Payload).FailedItems);
        Assert.Equal(scarce, failed.ProductId);
        Assert.Equal(StockFailureReasons.InsufficientStock, failed.Reason);
        Assert.Equal(3, failed.Requested);
        Assert.Equal(1, failed.Available);
    }

    [Fact]
    public async Task UnknownProduct_ReportsProductNotFoundWithZeroAvailable()
    {
        var missing = Guid.NewGuid();

        await Run(Order(Guid.NewGuid(), (missing, 2)));

        var queued = Assert.Single(_outbox.Items);
        var failed = Assert.Single(EventJson.Deserialize<StockRejected>(queued.Payload).FailedItems);
        Assert.Equal(StockFailureReasons.ProductNotFound, failed.Reason);
        Assert.Equal(0, failed.Available);
    }

    [Fact]
    public async Task SoftDeletedProduct_CountsAsProductNotFound()
    {
        var deleted = AddProduct("Gone", 10, deleted: true);

        await Run(Order(Guid.NewGuid(), (deleted, 1)));

        var queued = Assert.Single(_outbox.Items);
        var failed = Assert.Single(EventJson.Deserialize<StockRejected>(queued.Payload).FailedItems);
        Assert.Equal(StockFailureReasons.ProductNotFound, failed.Reason);
    }

    [Fact]
    public async Task TwoFailingItems_AreBothListed()
    {
        var shortA = AddProduct("A", 1);
        var shortB = AddProduct("B", 0);
        var ok = AddProduct("Ok", 9);

        await Run(Order(Guid.NewGuid(), (shortA, 5), (ok, 1), (shortB, 2)));

        var queued = Assert.Single(_outbox.Items);
        var failed = EventJson.Deserialize<StockRejected>(queued.Payload).FailedItems;
        Assert.Equal(2, failed.Count);
        Assert.Equal(9, _stock.StockOf(ok));
    }

    [Fact]
    public async Task SameEventDeliveredTwice_ProducesOneReservationAndOneOutboxRow()
    {
        var product = AddProduct("Tea", 5);
        var message = Order(Guid.NewGuid(), (product, 2));

        var first = await Run(message);
        var second = await Run(message);

        Assert.Equal(EventOutcome.Applied, first);
        Assert.Equal(EventOutcome.Duplicate, second);
        Assert.Single(_reservations.Items);
        Assert.Single(_outbox.Items);
        Assert.Equal(3, _stock.StockOf(product));
    }
}
