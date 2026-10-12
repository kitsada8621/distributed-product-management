using Catalog.Application.Common;
using Catalog.Application.Products;
using Catalog.Domain.Products;
using Catalog.UnitTests.Fakes;

namespace Catalog.UnitTests.Products;

public class ListProductsTests
{
    private static readonly DateTime Now = new(2026, 10, 10, 8, 0, 0, DateTimeKind.Utc);

    private readonly FakeProductRepository _repository = new();

    private static ListProductsRequest Request(
        string? q = null,
        string? categoryId = null,
        string? sort = null,
        string? page = null,
        string? pageSize = null,
        bool includeStock = false)
    {
        return new ListProductsRequest(q, categoryId, sort, page, pageSize, includeStock);
    }

    private Task<ListProductsResult> Run(ListProductsRequest request)
    {
        return new ListProducts(_repository).ExecuteAsync(request, CancellationToken.None);
    }

    [Fact]
    public async Task Execute_NoParameters_UsesDefaults()
    {
        var result = await Run(Request());

        var search = _repository.LastSearch!;
        Assert.Equal(ProductSort.NameAsc, search.Sort);
        Assert.Equal(1, search.Page);
        Assert.Equal(20, search.PageSize);
        Assert.Null(search.Query);
        Assert.Null(search.CategoryId);
        Assert.Equal(1, result.Page);
        Assert.Equal(20, result.PageSize);
    }

    [Theory]
    [InlineData("name_asc", ProductSort.NameAsc)]
    [InlineData("price_asc", ProductSort.PriceAsc)]
    [InlineData("price_desc", ProductSort.PriceDesc)]
    [InlineData("newest", ProductSort.Newest)]
    public async Task Execute_KnownSort_IsPassedToRepository(string sort, ProductSort expected)
    {
        await Run(Request(sort: sort));

        Assert.Equal(expected, _repository.LastSearch!.Sort);
    }

    [Fact]
    public async Task Execute_QueryAndCategory_AreTrimmedAndParsed()
    {
        var categoryId = Guid.NewGuid();

        await Run(Request(q: "  tea ", categoryId: categoryId.ToString()));

        Assert.Equal("tea", _repository.LastSearch!.Query);
        Assert.Equal(categoryId, _repository.LastSearch.CategoryId);
    }

    [Fact]
    public async Task Execute_BlankQuery_BecomesNull()
    {
        await Run(Request(q: "   "));

        Assert.Null(_repository.LastSearch!.Query);
    }

    [Theory]
    [InlineData("sort", "price_up", null, null, null)]
    [InlineData("pageSize", null, "101", null, null)]
    [InlineData("pageSize", null, "0", null, null)]
    [InlineData("pageSize", null, "abc", null, null)]
    [InlineData("page", null, null, "0", null)]
    [InlineData("page", null, null, "abc", null)]
    [InlineData("categoryId", null, null, null, "not-a-guid")]
    public async Task Execute_BadInput_ThrowsValidationErrorOnThatField(
        string field,
        string? sort,
        string? pageSize,
        string? page,
        string? categoryId)
    {
        var exception = await Assert.ThrowsAsync<CatalogException>(
            () => Run(Request(categoryId: categoryId, sort: sort, page: page, pageSize: pageSize)));

        Assert.Equal(ErrorCodes.ValidationError, exception.Code);
        Assert.Equal(field, Assert.Single(exception.Errors!).Field);
    }

    [Fact]
    public async Task Execute_SeveralBadFields_ReportsAllOfThem()
    {
        var exception = await Assert.ThrowsAsync<CatalogException>(
            () => Run(Request(sort: "x", page: "0", pageSize: "500")));

        var fields = exception.Errors!.Select(error => error.Field).Order().ToArray();
        Assert.Equal(["page", "pageSize", "sort"], fields);
    }

    [Theory]
    [InlineData(0, 20, 0)]
    [InlineData(1, 20, 1)]
    [InlineData(20, 20, 1)]
    [InlineData(21, 20, 2)]
    [InlineData(101, 10, 11)]
    public async Task Execute_ComputesTotalPages(long total, int pageSize, int expectedPages)
    {
        _repository.CannedPage = new ProductPage([], total);

        var result = await Run(Request(pageSize: pageSize.ToString()));

        Assert.Equal(total, result.Total);
        Assert.Equal(expectedPages, result.TotalPages);
    }

    [Fact]
    public async Task Execute_Guest_DoesNotSeeStock()
    {
        _repository.CannedPage = PageWithOneProduct(stock: 7);

        var result = await Run(Request(includeStock: false));

        var item = Assert.Single(result.Items);
        Assert.Null(item.Stock);
        Assert.True(item.InStock);
    }

    [Fact]
    public async Task Execute_Admin_SeesStockNumber()
    {
        _repository.CannedPage = PageWithOneProduct(stock: 7);

        var result = await Run(Request(includeStock: true));

        Assert.Equal(7, Assert.Single(result.Items).Stock);
    }

    [Fact]
    public async Task Execute_ZeroStock_IsNotInStock()
    {
        _repository.CannedPage = PageWithOneProduct(stock: 0);

        var result = await Run(Request());

        Assert.False(Assert.Single(result.Items).InStock);
    }

    [Fact]
    public async Task Execute_MapsPriceAndCategoryName()
    {
        _repository.CannedPage = PageWithOneProduct(stock: 1);

        var result = await Run(Request());

        var item = Assert.Single(result.Items);
        Assert.Equal(129.00m, item.Price);
        Assert.Equal("Drinks", item.CategoryName);
    }

    private static ProductPage PageWithOneProduct(int stock)
    {
        var product = Product.Create("Tea", "Green", 12900, Guid.NewGuid(), stock, Now);
        return new ProductPage([new ProductListItem(product, "Drinks")], 1);
    }
}
