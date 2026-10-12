using Catalog.Application.Stock;

namespace Catalog.UnitTests.Fakes;

public sealed class FakeStockRepository : IStockRepository
{
    private sealed record Entry(string Name, long PriceMinor, int Stock, bool IsDeleted);

    private Dictionary<Guid, Entry> _products = [];

    public void AddProduct(Guid id, string name, long priceMinor, int stock, bool isDeleted = false)
    {
        _products[id] = new Entry(name, priceMinor, stock, isDeleted);
    }

    public int StockOf(Guid id) => _products[id].Stock;

    // The fake transaction takes a copy when it begins and puts it back on rollback.
    public object TakeCopy() => new Dictionary<Guid, Entry>(_products);

    public void RestoreCopy(object copy) => _products = (Dictionary<Guid, Entry>)copy;

    public Task<bool> TryDeductAsync(Guid productId, int quantity, DateTime now, CancellationToken cancellationToken)
    {
        if (!_products.TryGetValue(productId, out var entry) || entry.IsDeleted || entry.Stock < quantity)
        {
            return Task.FromResult(false);
        }

        _products[productId] = entry with { Stock = entry.Stock - quantity };
        return Task.FromResult(true);
    }

    public Task<int?> GetActiveStockAsync(Guid productId, CancellationToken cancellationToken)
    {
        if (!_products.TryGetValue(productId, out var entry) || entry.IsDeleted)
        {
            return Task.FromResult<int?>(null);
        }

        return Task.FromResult<int?>(entry.Stock);
    }

    public Task RestoreAsync(Guid productId, int quantity, DateTime now, CancellationToken cancellationToken)
    {
        if (_products.TryGetValue(productId, out var entry))
        {
            _products[productId] = entry with { Stock = entry.Stock + quantity };
        }
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<ProductSnapshot>> GetSnapshotsAsync(
        IReadOnlyCollection<Guid> productIds,
        CancellationToken cancellationToken)
    {
        var snapshots = new List<ProductSnapshot>();
        foreach (var id in productIds)
        {
            if (_products.TryGetValue(id, out var entry))
            {
                snapshots.Add(new ProductSnapshot(id, entry.Name, entry.PriceMinor));
            }
        }
        return Task.FromResult<IReadOnlyList<ProductSnapshot>>(snapshots);
    }
}
