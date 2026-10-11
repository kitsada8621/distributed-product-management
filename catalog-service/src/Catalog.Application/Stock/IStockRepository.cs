namespace Catalog.Application.Stock;

public sealed record ProductSnapshot(Guid Id, string Name, long PriceMinor);

public interface IStockRepository
{
    // UPDATE ... WHERE is_deleted = 0 AND stock >= quantity; true when exactly one row changed.
    Task<bool> TryDeductAsync(Guid productId, int quantity, DateTime now, CancellationToken cancellationToken);

    // Null when the product is missing or soft-deleted.
    Task<int?> GetActiveStockAsync(Guid productId, CancellationToken cancellationToken);

    // Adds back even to a soft-deleted product, so a cancelled order never loses stock.
    Task RestoreAsync(Guid productId, int quantity, DateTime now, CancellationToken cancellationToken);

    Task<IReadOnlyList<ProductSnapshot>> GetSnapshotsAsync(
        IReadOnlyCollection<Guid> productIds,
        CancellationToken cancellationToken);
}
