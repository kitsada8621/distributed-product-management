using Catalog.Domain.Products;

namespace Catalog.Application.Products;

public interface IProductRepository
{
    // Includes soft-deleted products: their rows still reference the category.
    Task<IReadOnlyList<Product>> ListByCategoryAsync(Guid categoryId, CancellationToken cancellationToken);

    Task<ProductPage> SearchAsync(ProductSearch search, CancellationToken cancellationToken);

    Task<Product?> FindActiveAsync(Guid id, CancellationToken cancellationToken);

    Task AddAsync(Product product, CancellationToken cancellationToken);
}
