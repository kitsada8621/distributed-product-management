using Catalog.Application.Categories;
using Catalog.Application.Common;

namespace Catalog.Application.Products;

public sealed class GetProduct(IProductRepository products, ICategoryRepository categories)
{
    public async Task<ProductView> ExecuteAsync(Guid id, bool includeStock, CancellationToken cancellationToken)
    {
        var product = await products.FindActiveAsync(id, cancellationToken);
        if (product is null)
        {
            throw new CatalogException(ErrorCodes.NotFound, "Product not found");
        }

        string? categoryName = null;
        if (product.CategoryId is not null)
        {
            var category = await categories.FindActiveAsync(product.CategoryId.Value, cancellationToken);
            categoryName = category?.Name;
        }

        return ProductView.From(product, categoryName, includeStock);
    }
}
