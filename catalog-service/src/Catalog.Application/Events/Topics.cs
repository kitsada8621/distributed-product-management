namespace Catalog.Application.Events;

public static class Topics
{
    public const string OrderCreated = "order.created";
    public const string OrderConfirmed = "order.confirmed";
    public const string OrderCancelled = "order.cancelled";
    public const string StockReserved = "stock.reserved";
    public const string StockRejected = "stock.rejected";

    public static string DeadLetter(string topic) => "dlq." + topic;
}
