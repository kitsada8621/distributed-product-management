using Catalog.Application.Products;
using Catalog.Domain.Products;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Infrastructure.Persistence;

public sealed class ProductRepository(CatalogDbContext context) : IProductRepository
{
    // Member-init (not a constructor) so EF can translate ordering on Product members.
    private sealed class Row
    {
        public required Product Product { get; init; }
        public string? CategoryName { get; init; }
    }

    public async Task<IReadOnlyList<Product>> ListByCategoryAsync(Guid categoryId, CancellationToken cancellationToken)
    {
        return await context.Products
            .Where(p => p.CategoryId == categoryId)
            .ToListAsync(cancellationToken);
    }

    public Task<Product?> FindActiveAsync(Guid id, CancellationToken cancellationToken)
    {
        return context.Products.FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted, cancellationToken);
    }

    public async Task AddAsync(Product product, CancellationToken cancellationToken)
    {
        await context.Products.AddAsync(product, cancellationToken);
    }

    public async Task<ProductPage> SearchAsync(ProductSearch search, CancellationToken cancellationToken)
    {
        var rows = BuildRows(search);

        var total = await rows.LongCountAsync(cancellationToken);
        var page = await Sort(rows, search.Sort)
            .Skip((search.Page - 1) * search.PageSize)
            .Take(search.PageSize)
            .ToListAsync(cancellationToken);

        var items = new List<ProductListItem>();
        foreach (var row in page)
        {
            items.Add(new ProductListItem(row.Product, row.CategoryName));
        }
        return new ProductPage(items, total);
    }

    private IQueryable<Row> BuildRows(ProductSearch search)
    {
        var products = context.Products.AsNoTracking().Where(p => !p.IsDeleted);
        if (search.CategoryId is not null)
        {
            products = products.Where(p => p.CategoryId == search.CategoryId);
        }
        if (search.Query is not null)
        {
            var pattern = "%" + EscapeLike(search.Query) + "%";
            products = products.Where(p => EF.Functions.Like(p.Name, pattern, "\\"));
        }

        // Left join: a product whose category was deleted has no name to show.
        var activeCategories = context.Categories.AsNoTracking().Where(c => !c.IsDeleted);
        return from p in products
               join c in activeCategories on p.CategoryId equals c.Id into joined
               from c in joined.DefaultIfEmpty()
               select new Row { Product = p, CategoryName = c.Name };
    }

    // Id is the last key so rows with equal sort values keep a stable order across pages.
    private static IQueryable<Row> Sort(IQueryable<Row> rows, ProductSort sort)
    {
        return sort switch
        {
            ProductSort.PriceAsc => rows.OrderBy(r => r.Product.PriceMinor).ThenBy(r => r.Product.Id),
            ProductSort.PriceDesc => rows.OrderByDescending(r => r.Product.PriceMinor).ThenBy(r => r.Product.Id),
            ProductSort.Newest => rows.OrderByDescending(r => r.Product.CreatedAt).ThenBy(r => r.Product.Id),
            _ => rows.OrderBy(r => EF.Functions.Collate(r.Product.Name, "NOCASE")).ThenBy(r => r.Product.Id)
        };
    }

    private static string EscapeLike(string text)
    {
        return text.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_");
    }
}
