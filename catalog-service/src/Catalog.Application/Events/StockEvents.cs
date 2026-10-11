namespace Catalog.Application.Events;

public static class StockFailureReasons
{
    public const string InsufficientStock = "INSUFFICIENT_STOCK";
    public const string ProductNotFound = "PRODUCT_NOT_FOUND";
}

public sealed record StockReservedItem(Guid ProductId, string Name, decimal UnitPrice, int Quantity);

public sealed record StockReserved(
    Guid EventId,
    DateTime OccurredAt,
    int Version,
    Guid OrderId,
    IReadOnlyList<StockReservedItem> Items);

public sealed record StockFailedItem(Guid ProductId, string Reason, int Requested, int Available);

public sealed record StockRejected(
    Guid EventId,
    DateTime OccurredAt,
    int Version,
    Guid OrderId,
    IReadOnlyList<StockFailedItem> FailedItems);
