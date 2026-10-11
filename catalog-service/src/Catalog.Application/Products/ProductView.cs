using Catalog.Domain.Products;

namespace Catalog.Application.Products;

public sealed record ProductView(
    Guid Id,
    string Name,
    string Description,
    decimal Price,
    Guid? CategoryId,
    string? CategoryName,
    bool InStock,
    int? Stock)
{
    public static ProductView From(Product product, string? categoryName, bool includeStock)
    {
        return new ProductView(
            product.Id,
            product.Name,
            product.Description,
            PriceConverter.ToDecimal(product.PriceMinor),
            product.CategoryId,
            categoryName,
            product.IsInStock,
            includeStock ? product.Stock : null);
    }
}
