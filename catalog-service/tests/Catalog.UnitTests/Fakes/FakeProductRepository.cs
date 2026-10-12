using Catalog.Application.Products;
using Catalog.Domain.Products;

namespace Catalog.UnitTests.Fakes;

public sealed class FakeProductRepository : IProductRepository
{
    public List<Product> Items { get; } = [];
    public ProductSearch? LastSearch { get; private set; }
    public ProductPage CannedPage { get; set; } = new([], 0);

    public Task<IReadOnlyList<Product>> ListByCategoryAsync(Guid categoryId, CancellationToken cancellationToken)
    {
        IReadOnlyList<Product> found = Items.Where(product => product.CategoryId == categoryId).ToList();
        return Task.FromResult(found);
    }

    public Task<ProductPage> SearchAsync(ProductSearch search, CancellationToken cancellationToken)
    {
        LastSearch = search;
        return Task.FromResult(CannedPage);
    }

    public Task AddAsync(Product product, CancellationToken cancellationToken)
    {
        Items.Add(product);
        return Task.CompletedTask;
    }

    public Task<Product?> FindActiveAsync(Guid id, CancellationToken cancellationToken)
    {
        var found = Items.FirstOrDefault(product => product.Id == id && !product.IsDeleted);
        return Task.FromResult(found);
    }
}
