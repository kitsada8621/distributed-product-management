using System.Text.Json;
using Catalog.Api.Workers;
using Catalog.Application.Events;
using Catalog.Domain.Messaging;
using Catalog.Domain.Products;
using Catalog.Domain.Reservations;
using Catalog.Infrastructure.Persistence;
using Catalog.IntegrationTests.Support;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Catalog.IntegrationTests.Workers;

public class ConsumedMessageProcessorTests(CatalogApiFactory factory) : IClassFixture<CatalogApiFactory>
{
    private static readonly DateTime Now = new(2026, 10, 10, 8, 30, 0, DateTimeKind.Utc);

    private CatalogDbContext CreateContext()
    {
        // Touching Server starts the host, which creates and migrates the database.
        _ = factory.Server;
        var options = new DbContextOptionsBuilder<CatalogDbContext>()
            .UseSqlite(factory.ConnectionString)
            .AddInterceptors(new SqlitePragmaInterceptor())
            .Options;
        return new CatalogDbContext(options);
    }

    private ConsumedMessageProcessor Processor()
    {
        _ = factory.Server;
        return factory.Services.GetRequiredService<ConsumedMessageProcessor>();
    }

    private async Task<Guid> SeedProductAsync(int stock, long priceMinor = 129000)
    {
        await using var context = CreateContext();
        var product = Product.Create($"prod-{Guid.NewGuid():N}", "", priceMinor, null, stock, Now);
        context.Products.Add(product);
        await context.SaveChangesAsync();
        return product.Id;
    }

    private async Task<int> StockOfAsync(Guid productId)
    {
        await using var context = CreateContext();
        return await context.Products.AsNoTracking().Where(p => p.Id == productId).Select(p => p.Stock).SingleAsync();
    }

    private static ConsumedMessage Created(Guid eventId, Guid orderId, Guid productId, int quantity)
    {
        var body = new OrderCreated(eventId, Now, 1, orderId, "u1", [new OrderCreatedItem(productId, quantity)]);
        return new ConsumedMessage(Topics.OrderCreated, orderId.ToString(), EventJson.Serialize(body), "corr-1");
    }

    private static ConsumedMessage Confirmed(Guid orderId)
    {
        var body = new OrderConfirmed(Guid.NewGuid(), Now, 1, orderId);
        return new ConsumedMessage(Topics.OrderConfirmed, orderId.ToString(), EventJson.Serialize(body), "corr-1");
    }

    private static ConsumedMessage Cancelled(Guid orderId)
    {
        var body = new OrderCancelled(Guid.NewGuid(), Now, 1, orderId, "USER_CANCELLED");
        return new ConsumedMessage(Topics.OrderCancelled, orderId.ToString(), EventJson.Serialize(body), "corr-1");
    }

    private async Task<List<OutboxMessage>> OutboxFor(Guid orderId)
    {
        await using var context = CreateContext();
        return await context.OutboxMessages.AsNoTracking().Where(m => m.MessageKey == orderId.ToString()).ToListAsync();
    }

    private async Task<Reservation?> ReservationOf(Guid orderId)
    {
        await using var context = CreateContext();
        return await context.Reservations.AsNoTracking().Include(r => r.Items).FirstOrDefaultAsync(r => r.OrderId == orderId);
    }

    [Fact]
    public async Task OrderCreated_ReservesStockAndQueuesStockReserved()
    {
        var product = await SeedProductAsync(stock: 5);
        var orderId = Guid.NewGuid();

        await Processor().ProcessAsync(Created(Guid.NewGuid(), orderId, product, 2), CancellationToken.None);

        Assert.Equal(3, await StockOfAsync(product));
        Assert.Equal(ReservationStatus.Reserved, (await ReservationOf(orderId))!.Status);
        var queued = Assert.Single(await OutboxFor(orderId));
        Assert.Equal(Topics.StockReserved, queued.Topic);
    }

    [Fact]
    public async Task SameOrderCreatedTwice_ChangesNothingTheSecondTime()
    {
        var product = await SeedProductAsync(stock: 5);
        var orderId = Guid.NewGuid();
        var message = Created(Guid.NewGuid(), orderId, product, 2);

        await Processor().ProcessAsync(message, CancellationToken.None);
        await Processor().ProcessAsync(message, CancellationToken.None);

        Assert.Equal(3, await StockOfAsync(product));
        Assert.Single(await OutboxFor(orderId));
    }

    [Fact]
    public async Task OrderCreated_MoreThanStock_QueuesStockRejectedAndKeepsStock()
    {
        var product = await SeedProductAsync(stock: 1);
        var orderId = Guid.NewGuid();

        await Processor().ProcessAsync(Created(Guid.NewGuid(), orderId, product, 4), CancellationToken.None);

        Assert.Equal(1, await StockOfAsync(product));
        Assert.Null(await ReservationOf(orderId));
        var queued = Assert.Single(await OutboxFor(orderId));
        Assert.Equal(Topics.StockRejected, queued.Topic);
    }

    [Fact]
    public async Task OrderConfirmed_MovesReservationToCommitted()
    {
        var product = await SeedProductAsync(stock: 5);
        var orderId = Guid.NewGuid();
        await Processor().ProcessAsync(Created(Guid.NewGuid(), orderId, product, 2), CancellationToken.None);

        await Processor().ProcessAsync(Confirmed(orderId), CancellationToken.None);

        Assert.Equal(ReservationStatus.Committed, (await ReservationOf(orderId))!.Status);
        Assert.Equal(3, await StockOfAsync(product));
    }

    [Fact]
    public async Task OrderCancelled_AfterReserve_ReturnsStockAndReleases()
    {
        var product = await SeedProductAsync(stock: 5);
        var orderId = Guid.NewGuid();
        await Processor().ProcessAsync(Created(Guid.NewGuid(), orderId, product, 2), CancellationToken.None);

        await Processor().ProcessAsync(Cancelled(orderId), CancellationToken.None);

        Assert.Equal(5, await StockOfAsync(product));
        Assert.Equal(ReservationStatus.Released, (await ReservationOf(orderId))!.Status);
    }

    [Fact]
    public async Task OrderCancelled_BeforeOrderCreated_LeavesATombstoneSoNothingIsDeducted()
    {
        var product = await SeedProductAsync(stock: 5);
        var orderId = Guid.NewGuid();

        await Processor().ProcessAsync(Cancelled(orderId), CancellationToken.None);
        await Processor().ProcessAsync(Created(Guid.NewGuid(), orderId, product, 2), CancellationToken.None);

        Assert.Equal(5, await StockOfAsync(product));
        var tombstone = await ReservationOf(orderId);
        Assert.Equal(ReservationStatus.Released, tombstone!.Status);
        Assert.Empty(tombstone.Items);
        Assert.Empty(await OutboxFor(orderId));
    }

    [Fact]
    public async Task InvalidJson_IsTriedThreeTimesThenDeadLettered()
    {
        var orderId = Guid.NewGuid();
        var broken = new ConsumedMessage(Topics.OrderCreated, orderId.ToString(), "{ not json", "corr-9");

        await Processor().ProcessAsync(broken, CancellationToken.None);

        var queued = Assert.Single(await OutboxFor(orderId));
        Assert.Equal("dlq.order.created", queued.Topic);
        Assert.Equal("corr-9", queued.CorrelationId);
        using var document = JsonDocument.Parse(queued.Payload);
        var root = document.RootElement;
        Assert.Equal("{ not json", root.GetProperty("originalMessage").GetString());
        Assert.False(string.IsNullOrEmpty(root.GetProperty("error").GetString()));
        Assert.Equal(3, root.GetProperty("attempts").GetInt32());
    }

    [Fact]
    public void KafkaDisabled_NoConsumerWorkerIsRegistered()
    {
        _ = factory.Server;

        var hostedServices = factory.Services.GetServices<IHostedService>();

        Assert.DoesNotContain(hostedServices, service => service is OrderEventsConsumerWorker);
    }
}
