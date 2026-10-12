using Catalog.Application.Common;
using Catalog.Application.Products;
using Catalog.Domain.Categories;
using Catalog.Domain.Products;
using Catalog.UnitTests.Fakes;

namespace Catalog.UnitTests.Products;

public class GetProductTests
{
    private static readonly DateTime Now = new(2026, 10, 10, 8, 0, 0, DateTimeKind.Utc);

    private readonly FakeProductRepository _products = new();
    private readonly FakeCategoryRepository _categories = new();

    private Task<ProductView> Run(Guid id, bool includeStock = false)
    {
        return new GetProduct(_products, _categories).ExecuteAsync(id, includeStock, CancellationToken.None);
    }

    [Fact]
    public async Task Execute_ProductWithCategory_ReturnsCategoryName()
    {
        var category = Category.Create("Drinks", Now);
        _categories.Items.Add(category);
        var product = Product.Create("Tea", "Green", 12900, category.Id, 3, Now);
        _products.Items.Add(product);

        var view = await Run(product.Id);

        Assert.Equal("Tea", view.Name);
        Assert.Equal(129.00m, view.Price);
        Assert.Equal(category.Id, view.CategoryId);
        Assert.Equal("Drinks", view.CategoryName);
        Assert.True(view.InStock);
    }

    [Fact]
    public async Task Execute_ProductWithoutCategory_HasNullCategoryFields()
    {
        var product = Product.Create("Tea", "", 100, null, 0, Now);
        _products.Items.Add(product);

        var view = await Run(product.Id);

        Assert.Null(view.CategoryId);
        Assert.Null(view.CategoryName);
        Assert.False(view.InStock);
    }

    [Fact]
    public async Task Execute_MissingProduct_ThrowsNotFound()
    {
        var exception = await Assert.ThrowsAsync<CatalogException>(() => Run(Guid.NewGuid()));

        Assert.Equal(ErrorCodes.NotFound, exception.Code);
    }

    [Fact]
    public async Task Execute_SoftDeletedProduct_ThrowsNotFound()
    {
        var product = Product.Create("Tea", "", 100, null, 1, Now);
        product.SoftDelete(Now);
        _products.Items.Add(product);

        var exception = await Assert.ThrowsAsync<CatalogException>(() => Run(product.Id));

        Assert.Equal(ErrorCodes.NotFound, exception.Code);
    }

    [Fact]
    public async Task Execute_StockIsVisibleOnlyWhenRequested()
    {
        var product = Product.Create("Tea", "", 100, null, 9, Now);
        _products.Items.Add(product);

        var guestView = await Run(product.Id, includeStock: false);
        var adminView = await Run(product.Id, includeStock: true);

        Assert.Null(guestView.Stock);
        Assert.Equal(9, adminView.Stock);
    }
}
