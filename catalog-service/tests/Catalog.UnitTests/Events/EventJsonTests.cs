using System.Text.Json;
using Catalog.Application.Events;

namespace Catalog.UnitTests.Events;

public class EventJsonTests
{
    private static readonly DateTime At = new(2026, 10, 10, 8, 30, 0, DateTimeKind.Utc);

    [Fact]
    public void Deserialize_OrderCreated_ReadsAllFields()
    {
        var eventId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var json = $$"""
            {"eventId":"{{eventId}}","occurredAt":"2026-10-10T08:30:00Z","version":1,"orderId":"{{orderId}}","userId":"u1","items":[{"productId":"{{productId}}","quantity":2}]}
            """;

        var message = EventJson.Deserialize<OrderCreated>(json);

        Assert.Equal(eventId, message.EventId);
        Assert.Equal(At, message.OccurredAt);
        Assert.Equal(1, message.Version);
        Assert.Equal(orderId, message.OrderId);
        Assert.Equal("u1", message.UserId);
        var item = Assert.Single(message.Items);
        Assert.Equal(productId, item.ProductId);
        Assert.Equal(2, item.Quantity);
    }

    [Fact]
    public void Deserialize_OrderCancelled_ReadsReason()
    {
        var json = $$"""
            {"eventId":"{{Guid.NewGuid()}}","occurredAt":"2026-10-10T08:30:00Z","version":1,"orderId":"{{Guid.NewGuid()}}","reason":"TIMEOUT"}
            """;

        var message = EventJson.Deserialize<OrderCancelled>(json);

        Assert.Equal("TIMEOUT", message.Reason);
    }

    [Fact]
    public void Serialize_StockReserved_UsesCamelCaseAndNumericUnitPrice()
    {
        var orderId = Guid.NewGuid();
        var item = new StockReservedItem(Guid.NewGuid(), "Tea", 1290.00m, 2);
        var message = new StockReserved(Guid.NewGuid(), At, 1, orderId, [item]);

        var json = EventJson.Serialize(message);

        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        Assert.Equal(message.EventId, root.GetProperty("eventId").GetGuid());
        Assert.Equal("2026-10-10T08:30:00Z", root.GetProperty("occurredAt").GetString());
        Assert.Equal(1, root.GetProperty("version").GetInt32());
        Assert.Equal(orderId, root.GetProperty("orderId").GetGuid());
        var first = root.GetProperty("items")[0];
        Assert.Equal(item.ProductId, first.GetProperty("productId").GetGuid());
        Assert.Equal("Tea", first.GetProperty("name").GetString());
        Assert.Equal(JsonValueKind.Number, first.GetProperty("unitPrice").ValueKind);
        Assert.Equal(1290.00m, first.GetProperty("unitPrice").GetDecimal());
        Assert.Equal(2, first.GetProperty("quantity").GetInt32());
    }

    [Fact]
    public void Serialize_StockRejected_UsesFailedItemsKeys()
    {
        var failed = new StockFailedItem(Guid.NewGuid(), StockFailureReasons.InsufficientStock, 5, 2);
        var message = new StockRejected(Guid.NewGuid(), At, 1, Guid.NewGuid(), [failed]);

        var json = EventJson.Serialize(message);

        using var document = JsonDocument.Parse(json);
        var first = document.RootElement.GetProperty("failedItems")[0];
        Assert.Equal(failed.ProductId, first.GetProperty("productId").GetGuid());
        Assert.Equal("INSUFFICIENT_STOCK", first.GetProperty("reason").GetString());
        Assert.Equal(5, first.GetProperty("requested").GetInt32());
        Assert.Equal(2, first.GetProperty("available").GetInt32());
    }

    [Fact]
    public void DeadLetterTopic_PrefixesTheOriginalTopic()
    {
        Assert.Equal("dlq.order.created", Topics.DeadLetter(Topics.OrderCreated));
    }
}
