using Catalog.Application.Common;

namespace Catalog.Application.Products;

public sealed record ListProductsRequest(
    string? Q,
    string? CategoryId,
    string? Sort,
    string? Page,
    string? PageSize,
    bool IncludeStock);

public sealed record ListProductsResult(
    IReadOnlyList<ProductView> Items,
    int Page,
    int PageSize,
    long Total,
    int TotalPages);

public sealed class ListProducts(IProductRepository products)
{
    private const int DefaultPage = 1;
    private const int DefaultPageSize = 20;
    private const int MaxPageSize = 100;

    public async Task<ListProductsResult> ExecuteAsync(ListProductsRequest request, CancellationToken cancellationToken)
    {
        var search = ParseSearch(request);

        var page = await products.SearchAsync(search, cancellationToken);

        var items = new List<ProductView>();
        foreach (var item in page.Items)
        {
            items.Add(ProductView.From(item.Product, item.CategoryName, request.IncludeStock));
        }

        var totalPages = (int)Math.Ceiling(page.Total / (double)search.PageSize);
        return new ListProductsResult(items, search.Page, search.PageSize, page.Total, totalPages);
    }

    private static ProductSearch ParseSearch(ListProductsRequest request)
    {
        var errors = new List<FieldError>();
        var sort = ParseSort(request.Sort, errors);
        var categoryId = ParseCategoryId(request.CategoryId, errors);
        var page = ParseNumber(request.Page, "page", DefaultPage, 1, int.MaxValue, errors);
        var pageSize = ParseNumber(request.PageSize, "pageSize", DefaultPageSize, 1, MaxPageSize, errors);

        if (errors.Count > 0)
        {
            throw new CatalogException(ErrorCodes.ValidationError, "Invalid input", errors);
        }

        var query = string.IsNullOrWhiteSpace(request.Q) ? null : request.Q.Trim();
        return new ProductSearch(query, categoryId, sort, page, pageSize);
    }

    private static ProductSort ParseSort(string? value, List<FieldError> errors)
    {
        switch (value)
        {
            case null:
            case "":
            case "name_asc":
                return ProductSort.NameAsc;
            case "price_asc":
                return ProductSort.PriceAsc;
            case "price_desc":
                return ProductSort.PriceDesc;
            case "newest":
                return ProductSort.Newest;
            default:
                errors.Add(new FieldError("sort", ErrorCodes.InvalidFormat, "Unknown sort value"));
                return ProductSort.NameAsc;
        }
    }

    private static Guid? ParseCategoryId(string? value, List<FieldError> errors)
    {
        if (string.IsNullOrEmpty(value))
        {
            return null;
        }

        if (!Guid.TryParse(value, out var categoryId))
        {
            errors.Add(new FieldError("categoryId", ErrorCodes.InvalidFormat, "categoryId must be a UUID"));
            return null;
        }

        return categoryId;
    }

    private static int ParseNumber(
        string? value,
        string field,
        int defaultValue,
        int min,
        int max,
        List<FieldError> errors)
    {
        if (string.IsNullOrEmpty(value))
        {
            return defaultValue;
        }

        if (!int.TryParse(value, out var number))
        {
            errors.Add(new FieldError(field, ErrorCodes.InvalidFormat, $"{field} must be an integer"));
            return defaultValue;
        }

        if (number < min || number > max)
        {
            errors.Add(new FieldError(field, ErrorCodes.OutOfRange, $"{field} must be between {min} and {max}"));
            return defaultValue;
        }

        return number;
    }
}
