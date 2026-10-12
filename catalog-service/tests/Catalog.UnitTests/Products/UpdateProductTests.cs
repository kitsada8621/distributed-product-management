using Catalog.Application.Common;
using Catalog.Application.Products;
using Catalog.Domain.Categories;
using Catalog.Domain.Products;
using Catalog.UnitTests.Fakes;

namespace Catalog.UnitTests.Products;

public class UpdateProductTests
{
    private readonly FakeProductRepository _products = new();
    private readonly FakeCategoryRepository _categories = new();
    private readonly FakeUnitOfWork _unitOfWork = new();
    private readonly FakeTimeProvider _timeProvider = new();

    private DateTime Now => _timeProvider.GetUtcNow().UtcDateTime;

    private Task<ProductView> Run(Guid id, UpdateProductRequest request)
    {
        var useCase = new UpdateProduct(_products, _categories, _unitOfWork, _timeProvider);
        return useCase.ExecuteAsync(id, request, CancellationToken.None);
    }

    private Product AddProduct(Guid? categoryId = null, int stock = 9)
    {
        var product = Product.Create("Tea", "Green", 12900, categoryId, stock, Now);
        _products.Items.Add(product);
        return product;
    }

    [Fact]
    public async Task Execute_ValidInput_ChangesFieldsAndKeepsStock()
    {
        var category = Category.Create("Drinks", Now);
        _categories.Items.Add(category);
        var product = AddProduct(stock: 9);

        var view = await Run(product.Id, new UpdateProductRequest("Coffee", "Dark", 55.50m, category.Id));

        Assert.Equal("Coffee", product.Name);
        Assert.Equal("Dark", product.Description);
        Assert.Equal(5550, product.PriceMinor);
        Assert.Equal(category.Id, product.CategoryId);
        Assert.Equal(9, product.Stock);
        Assert.Equal("Drinks", view.CategoryName);
        Assert.Equal(9, view.Stock);
        Assert.Equal(1, _unitOfWork.SaveCount);
    }

    [Fact]
    public async Task Execute_NullDescription_BecomesEmptyString()
    {
        var product = AddProduct();

        await Run(product.Id, new UpdateProductRequest("Tea", null, 10m, null));

        Assert.Equal(string.Empty, product.Description);
    }

    [Fact]
    public async Task Execute_NullCategoryId_ClearsCategory()
    {
        var product = AddProduct(categoryId: Guid.NewGuid());

        var view = await Run(product.Id, new UpdateProductRequest("Tea", "", 10m, null));

        Assert.Null(product.CategoryId);
        Assert.Null(view.CategoryName);
    }

    [Fact]
    public async Task Execute_MissingProduct_ThrowsNotFound()
    {
        var exception = await Assert.ThrowsAsync<CatalogException>(
            () => Run(Guid.NewGuid(), new UpdateProductRequest("Tea", "", 10m, null)));

        Assert.Equal(ErrorCodes.NotFound, exception.Code);
    }

    [Fact]
    public async Task Execute_SoftDeletedProduct_ThrowsNotFound()
    {
        var product = AddProduct();
        product.SoftDelete(Now);

        var exception = await Assert.ThrowsAsync<CatalogException>(
            () => Run(product.Id, new UpdateProductRequest("Tea", "", 10m, null)));

        Assert.Equal(ErrorCodes.NotFound, exception.Code);
    }

    [Fact]
    public async Task Execute_InvalidFields_ThrowsValidationErrorAndChangesNothing()
    {
        var product = AddProduct();

        var exception = await Assert.ThrowsAsync<CatalogException>(
            () => Run(product.Id, new UpdateProductRequest("", "", 0m, Guid.NewGuid())));

        Assert.Equal(ErrorCodes.ValidationError, exception.Code);
        var fields = exception.Errors!.Select(error => error.Field).Order().ToArray();
        Assert.Equal(["categoryId", "name", "price"], fields);
        Assert.Equal("Tea", product.Name);
        Assert.Equal(0, _unitOfWork.SaveCount);
    }
}
