using Catalog.Domain.Products;

namespace Catalog.Application.Products;

public enum ProductSort
{
    NameAsc,
    PriceAsc,
    PriceDesc,
    Newest
}

public sealed record ProductSearch(string? Query, Guid? CategoryId, ProductSort Sort, int Page, int PageSize);

public sealed record ProductListItem(Product Product, string? CategoryName);

public sealed record ProductPage(IReadOnlyList<ProductListItem> Items, long Total);
