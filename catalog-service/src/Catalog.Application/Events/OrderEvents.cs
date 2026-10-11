namespace Catalog.Application.Events;

public sealed record OrderCreatedItem(Guid ProductId, int Quantity);

public sealed record OrderCreated(
    Guid EventId,
    DateTime OccurredAt,
    int Version,
    Guid OrderId,
    string UserId,
    IReadOnlyList<OrderCreatedItem> Items);

public sealed record OrderConfirmed(Guid EventId, DateTime OccurredAt, int Version, Guid OrderId);

public sealed record OrderCancelled(Guid EventId, DateTime OccurredAt, int Version, Guid OrderId, string Reason);
