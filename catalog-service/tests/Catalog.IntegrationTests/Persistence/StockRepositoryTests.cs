using Catalog.Domain.Products;
using Catalog.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Catalog.IntegrationTests.Persistence;

public sealed class StockRepositoryTests : IDisposable
{
    private static readonly DateTime Now = new(2026, 10, 10, 8, 30, 0, DateTimeKind.Utc);

    private readonly TestDatabase _database = new();

    public void Dispose()
    {
        _database.Dispose();
    }

    private async Task<Guid> SeedAsync(int stock, bool deleted = false, string name = "Tea", long priceMinor = 12900)
    {
        await using var context = _database.CreateContext();
        var product = Product.Create(name, "", priceMinor, null, stock, Now);
        if (deleted)
        {
            product.SoftDelete(Now);
        }
        context.Products.Add(product);
        await context.SaveChangesAsync();
        return product.Id;
    }

    private async Task<int> StockOfAsync(Guid id)
    {
        await using var context = _database.CreateContext();
        return await context.Products.AsNoTracking().Where(p => p.Id == id).Select(p => p.Stock).SingleAsync();
    }

    private async Task<bool> DeductAsync(Guid id, int quantity)
    {
        await using var context = _database.CreateContext();
        return await new StockRepository(context).TryDeductAsync(id, quantity, Now, CancellationToken.None);
    }

    [Fact]
    public async Task Deduct_EnoughStock_ReducesIt()
    {
        var id = await SeedAsync(stock: 5);

        Assert.True(await DeductAsync(id, 3));

        Assert.Equal(2, await StockOfAsync(id));
    }

    [Fact]
    public async Task Deduct_ExactlyTheRemainingStock_LeavesZero()
    {
        var id = await SeedAsync(stock: 4);

        Assert.True(await DeductAsync(id, 4));

        Assert.Equal(0, await StockOfAsync(id));
    }

    [Fact]
    public async Task Deduct_NotEnoughStock_ChangesNothing()
    {
        var id = await SeedAsync(stock: 2);

        Assert.False(await DeductAsync(id, 3));

        Assert.Equal(2, await StockOfAsync(id));
    }

    [Fact]
    public async Task Deduct_SoftDeletedProduct_ChangesNothing()
    {
        var id = await SeedAsync(stock: 9, deleted: true);

        Assert.False(await DeductAsync(id, 1));

        Assert.Equal(9, await StockOfAsync(id));
    }

    [Fact]
    public async Task Deduct_UnknownProduct_ReturnsFalse()
    {
        Assert.False(await DeductAsync(Guid.NewGuid(), 1));
    }

    [Fact]
    public async Task Deduct_TwentyParallelOrdersOfOneAgainstTen_SucceedExactlyTenTimes()
    {
        var id = await SeedAsync(stock: 10);

        var attempts = Enumerable.Range(0, 20).Select(_ => Task.Run(() => DeductAsync(id, 1)));
        var results = await Task.WhenAll(attempts);

        Assert.Equal(10, results.Count(succeeded => succeeded));
        Assert.Equal(0, await StockOfAsync(id));
    }

    [Fact]
    public async Task Deduct_InsideRolledBackTransaction_LeavesStockUnchanged()
    {
        var id = await SeedAsync(stock: 5);
        await using (var context = _database.CreateContext())
        {
            await using var transaction = await context.BeginAsync(CancellationToken.None);
            await new StockRepository(context).TryDeductAsync(id, 3, Now, CancellationToken.None);
            await transaction.RollbackAsync(CancellationToken.None);
        }

        Assert.Equal(5, await StockOfAsync(id));
    }

    [Fact]
    public async Task Restore_AddsQuantityEvenToSoftDeletedProduct()
    {
        var id = await SeedAsync(stock: 1, deleted: true);
        await using (var context = _database.CreateContext())
        {
            await new StockRepository(context).RestoreAsync(id, 4, Now, CancellationToken.None);
        }

        Assert.Equal(5, await StockOfAsync(id));
    }

    [Fact]
    public async Task Snapshots_ReturnNameAndPriceAndSkipDeletedProducts()
    {
        var live = await SeedAsync(stock: 1, name: "Live", priceMinor: 5050);
        var gone = await SeedAsync(stock: 1, deleted: true, name: "Gone");
        await using var context = _database.CreateContext();

        var snapshots = await new StockRepository(context)
            .GetSnapshotsAsync([live, gone, Guid.NewGuid()], CancellationToken.None);

        var snapshot = Assert.Single(snapshots);
        Assert.Equal(live, snapshot.Id);
        Assert.Equal("Live", snapshot.Name);
        Assert.Equal(5050, snapshot.PriceMinor);
    }

    [Fact]
    public async Task GetActiveStock_ReturnsStockOrNullForDeletedAndUnknown()
    {
        var live = await SeedAsync(stock: 7);
        var gone = await SeedAsync(stock: 7, deleted: true);
        await using var context = _database.CreateContext();
        var repository = new StockRepository(context);

        Assert.Equal(7, await repository.GetActiveStockAsync(live, CancellationToken.None));
        Assert.Null(await repository.GetActiveStockAsync(gone, CancellationToken.None));
        Assert.Null(await repository.GetActiveStockAsync(Guid.NewGuid(), CancellationToken.None));
    }
}
