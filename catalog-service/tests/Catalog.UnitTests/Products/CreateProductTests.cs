using Catalog.Application.Common;
using Catalog.Application.Products;
using Catalog.Domain.Categories;
using Catalog.UnitTests.Fakes;

namespace Catalog.UnitTests.Products;

public class CreateProductTests
{
    private readonly FakeProductRepository _products = new();
    private readonly FakeCategoryRepository _categories = new();
    private readonly FakeUnitOfWork _unitOfWork = new();
    private readonly FakeTimeProvider _timeProvider = new();

    private Task<ProductView> Run(CreateProductRequest request)
    {
        var useCase = new CreateProduct(_products, _categories, _unitOfWork, _timeProvider);
        return useCase.ExecuteAsync(request, CancellationToken.None);
    }

    private static CreateProductRequest Valid(
        string? name = "Tea",
        decimal? price = 1290.00m,
        Guid? categoryId = null,
        decimal? stock = 5)
    {
        return new CreateProductRequest(name, "Green tea", price, categoryId, stock);
    }

    private static async Task<CatalogException> RunExpectingError(Func<Task<ProductView>> action)
    {
        return await Assert.ThrowsAsync<CatalogException>(action);
    }

    private Category AddCategory(string name)
    {
        var category = Category.Create(name, _timeProvider.GetUtcNow().UtcDateTime);
        _categories.Items.Add(category);
        return category;
    }

    [Fact]
    public async Task Execute_ValidInput_StoresPriceInSatangAndSavesOnce()
    {
        var category = AddCategory("Drinks");

        var view = await Run(Valid(categoryId: category.Id));

        var stored = Assert.Single(_products.Items);
        Assert.Equal(129000, stored.PriceMinor);
        Assert.Equal(5, stored.Stock);
        Assert.Equal(1290.00m, view.Price);
        Assert.True(view.InStock);
        Assert.Equal(5, view.Stock);
        Assert.Equal("Drinks", view.CategoryName);
        Assert.Equal(1, _unitOfWork.SaveCount);
    }

    [Fact]
    public async Task Execute_NoCategory_IsAllowed()
    {
        var view = await Run(Valid());

        Assert.Null(view.CategoryId);
        Assert.Null(view.CategoryName);
    }

    [Fact]
    public async Task Execute_UnknownCategory_ThrowsValidationErrorOnCategoryId()
    {
        var exception = await RunExpectingError(() => Run(Valid(categoryId: Guid.NewGuid())));

        Assert.Equal(ErrorCodes.ValidationError, exception.Code);
        Assert.Equal("categoryId", Assert.Single(exception.Errors!).Field);
        Assert.Equal(0, _unitOfWork.SaveCount);
    }

    [Fact]
    public async Task Execute_DeletedCategory_ThrowsValidationErrorOnCategoryId()
    {
        var category = AddCategory("Drinks");
        category.SoftDelete(_timeProvider.GetUtcNow().UtcDateTime);

        var exception = await RunExpectingError(() => Run(Valid(categoryId: category.Id)));

        Assert.Equal("categoryId", Assert.Single(exception.Errors!).Field);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(12.345)]
    public async Task Execute_BadPrice_ThrowsValidationErrorOnPrice(double price)
    {
        var exception = await RunExpectingError(() => Run(Valid(price: (decimal)price)));

        Assert.Equal("price", Assert.Single(exception.Errors!).Field);
    }

    [Fact]
    public async Task Execute_MissingPrice_ThrowsRequiredOnPrice()
    {
        var exception = await RunExpectingError(() => Run(Valid(price: null)));

        var error = Assert.Single(exception.Errors!);
        Assert.Equal("price", error.Field);
        Assert.Equal(ErrorCodes.Required, error.Code);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("  ")]
    public async Task Execute_EmptyName_ThrowsRequiredOnName(string? name)
    {
        var exception = await RunExpectingError(() => Run(Valid(name: name)));

        var error = Assert.Single(exception.Errors!);
        Assert.Equal("name", error.Field);
        Assert.Equal(ErrorCodes.Required, error.Code);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(1.5)]
    public async Task Execute_BadStock_ThrowsValidationErrorOnStock(double stock)
    {
        var exception = await RunExpectingError(() => Run(Valid(stock: (decimal)stock)));

        Assert.Equal("stock", Assert.Single(exception.Errors!).Field);
    }

    [Fact]
    public async Task Execute_MissingStock_ThrowsRequiredOnStock()
    {
        var exception = await RunExpectingError(() => Run(Valid(stock: null)));

        var error = Assert.Single(exception.Errors!);
        Assert.Equal("stock", error.Field);
        Assert.Equal(ErrorCodes.Required, error.Code);
    }

    [Fact]
    public async Task Execute_ZeroStock_IsAllowedAndNotInStock()
    {
        var view = await Run(Valid(stock: 0));

        Assert.False(view.InStock);
        Assert.Equal(0, view.Stock);
    }

    [Fact]
    public async Task Execute_SeveralBadFields_ReportsAllTogether()
    {
        var exception = await RunExpectingError(() => Run(Valid(name: "", price: 0, stock: -1)));

        var fields = exception.Errors!.Select(error => error.Field).Order().ToArray();
        Assert.Equal(["name", "price", "stock"], fields);
    }
}
