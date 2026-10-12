using System.Text.Json;
using Catalog.Api.Workers;
using Catalog.Application.Events;
using Catalog.Application.Messaging;
using Catalog.Domain.Messaging;
using Catalog.Domain.Products;
using Catalog.Infrastructure.Kafka;
using Catalog.Infrastructure.Persistence;
using Catalog.IntegrationTests.Support;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging.Abstractions;

namespace Catalog.IntegrationTests.Workers;

// Each test builds its own factory (own database) because a tick publishes every due row it finds.
public sealed class OutboxRelayWorkerTests : IDisposable
{
    private readonly CatalogApiFactory _baseFactory = new();
    private readonly RecordingEventPublisher _publisher = new();
    private readonly FakeTimeProvider _clock = new();
    private readonly WebApplicationFactory<Program> _host;

    public OutboxRelayWorkerTests()
    {
        // With Kafka off, Program registers neither the publisher nor PublishPendingEvents, so the test adds them.
        _host = _baseFactory.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<TimeProvider>();
            services.AddSingleton<TimeProvider>(_clock);
            services.AddSingleton<IEventPublisher>(_publisher);
            services.AddScoped<PublishPendingEvents>();
        }));
        _ = _host.Server;
    }

    public void Dispose()
    {
        _host.Dispose();
        _baseFactory.Dispose();
    }

    private OutboxRelayWorker CreateWorker()
    {
        return new OutboxRelayWorker(
            _host.Services.GetRequiredService<IServiceScopeFactory>(),
            _host.Services.GetRequiredService<KafkaOptions>(),
            NullLogger<OutboxRelayWorker>.Instance);
    }

    private async Task<Guid> QueueAsync(string key = "order-1")
    {
        await using var scope = _host.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
        var message = OutboxMessage.Create(
            Guid.NewGuid(), "stock.reserved", key, "{\"x\":1}", "corr-3", _clock.GetUtcNow().UtcDateTime);
        context.OutboxMessages.Add(message);
        await context.SaveChangesAsync();
        return message.Id;
    }

    private async Task<OutboxMessage> LoadAsync(Guid id)
    {
        await using var scope = _host.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
        return await context.OutboxMessages.AsNoTracking().SingleAsync(m => m.Id == id);
    }

    [Fact]
    public async Task PendingRow_IsPublishedAndMarkedSent()
    {
        var id = await QueueAsync("order-7");

        await CreateWorker().RunOnceAsync(CancellationToken.None);

        var published = Assert.Single(_publisher.Published);
        Assert.Equal(new RecordedMessage("stock.reserved", "order-7", "{\"x\":1}", "corr-3"), published);
        var row = await LoadAsync(id);
        Assert.Equal(OutboxStatus.Sent, row.Status);
        Assert.Equal(_clock.GetUtcNow().UtcDateTime, row.SentAt);
    }

    [Fact]
    public async Task PublisherFails_RowStaysPendingWithOneSecondBackoffAndIsRetriedLater()
    {
        var id = await QueueAsync();
        var queuedAt = _clock.GetUtcNow().UtcDateTime;
        _publisher.FailuresRemaining = 1;
        var worker = CreateWorker();

        await worker.RunOnceAsync(CancellationToken.None);

        var failed = await LoadAsync(id);
        Assert.Equal(OutboxStatus.Pending, failed.Status);
        Assert.Equal(1, failed.Attempts);
        Assert.Equal(queuedAt.AddSeconds(1), failed.NextAttemptAt);

        await worker.RunOnceAsync(CancellationToken.None);
        Assert.Empty(_publisher.Published);

        _clock.Advance(TimeSpan.FromSeconds(1));
        await worker.RunOnceAsync(CancellationToken.None);
        Assert.Single(_publisher.Published);
        Assert.Equal(OutboxStatus.Sent, (await LoadAsync(id)).Status);
    }

    [Fact]
    public async Task TwoFailures_GiveATwoSecondDelay()
    {
        var id = await QueueAsync();
        _publisher.FailuresRemaining = 2;
        var worker = CreateWorker();
        await worker.RunOnceAsync(CancellationToken.None);
        _clock.Advance(TimeSpan.FromSeconds(1));

        await worker.RunOnceAsync(CancellationToken.None);

        var row = await LoadAsync(id);
        Assert.Equal(2, row.Attempts);
        Assert.Equal(_clock.GetUtcNow().UtcDateTime.AddSeconds(2), row.NextAttemptAt);
    }

    [Fact]
    public async Task RowWrittenByReserveStock_CarriesEventIdEqualToOutboxIdOccurredAtAndVersion()
    {
        var productId = await SeedProductAsync();
        var orderId = Guid.NewGuid();
        var created = new OrderCreated(
            Guid.NewGuid(), _clock.GetUtcNow().UtcDateTime, 1, orderId, "u1", [new OrderCreatedItem(productId, 1)]);
        var processor = _host.Services.GetRequiredService<ConsumedMessageProcessor>();
        await processor.ProcessAsync(
            new ConsumedMessage(Topics.OrderCreated, orderId.ToString(), EventJson.Serialize(created), "corr-5"),
            CancellationToken.None);

        await CreateWorker().RunOnceAsync(CancellationToken.None);

        var published = Assert.Single(_publisher.Published);
        Assert.Equal(Topics.StockReserved, published.Topic);
        Assert.Equal("corr-5", published.CorrelationId);
        using var document = JsonDocument.Parse(published.Payload);
        var root = document.RootElement;
        var eventId = root.GetProperty("eventId").GetGuid();
        Assert.Equal(1, root.GetProperty("version").GetInt32());
        Assert.True(root.TryGetProperty("occurredAt", out _));
        Assert.Equal(OutboxStatus.Sent, (await LoadAsync(eventId)).Status);
    }

    private async Task<Guid> SeedProductAsync()
    {
        await using var scope = _host.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
        var product = Product.Create($"prod-{Guid.NewGuid():N}", "", 1000, null, 5, _clock.GetUtcNow().UtcDateTime);
        context.Products.Add(product);
        await context.SaveChangesAsync();
        return product.Id;
    }
}
