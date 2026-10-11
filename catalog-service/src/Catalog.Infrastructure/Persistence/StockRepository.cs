using Catalog.Application.Stock;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Infrastructure.Persistence;

public sealed class StockRepository(CatalogDbContext context) : IStockRepository
{
    // One conditional UPDATE keeps stock from going below zero when orders race (API-SPEC P6, no overselling).
    public async Task<bool> TryDeductAsync(
        Guid productId,
        int quantity,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var rows = await context.Products
            .Where(p => p.Id == productId && !p.IsDeleted && p.Stock >= quantity)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(p => p.Stock, p => p.Stock - quantity)
                    .SetProperty(p => p.UpdatedAt, now),
                cancellationToken);
        return rows == 1;
    }

    public async Task<int?> GetActiveStockAsync(Guid productId, CancellationToken cancellationToken)
    {
        return await context.Products
            .AsNoTracking()
            .Where(p => p.Id == productId && !p.IsDeleted)
            .Select(p => (int?)p.Stock)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task RestoreAsync(Guid productId, int quantity, DateTime now, CancellationToken cancellationToken)
    {
        await context.Products
            .Where(p => p.Id == productId)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(p => p.Stock, p => p.Stock + quantity)
                    .SetProperty(p => p.UpdatedAt, now),
                cancellationToken);
    }

    public async Task<IReadOnlyList<ProductSnapshot>> GetSnapshotsAsync(
        IReadOnlyCollection<Guid> productIds,
        CancellationToken cancellationToken)
    {
        return await context.Products
            .AsNoTracking()
            .Where(p => productIds.Contains(p.Id) && !p.IsDeleted)
            .Select(p => new ProductSnapshot(p.Id, p.Name, p.PriceMinor))
            .ToListAsync(cancellationToken);
    }
}
