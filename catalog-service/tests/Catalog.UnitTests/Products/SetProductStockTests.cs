using Catalog.Application.Common;
using Catalog.Application.Products;
using Catalog.Domain.Products;
using Catalog.UnitTests.Fakes;

namespace Catalog.UnitTests.Products;

public class SetProductStockTests
{
    private readonly FakeProductRepository _products = new();
    private readonly FakeUnitOfWork _unitOfWork = new();
    private readonly FakeTimeProvider _timeProvider = new();

    private Product AddProduct(int stock = 5)
    {
        var product = Product.Create("Tea", "", 100, null, stock, _timeProvider.GetUtcNow().UtcDateTime);
        _products.Items.Add(product);
        return product;
    }

    private Task<SetStockResult> Run(Guid id, decimal? quantity)
    {
        var useCase = new SetProductStock(_products, _unitOfWork, _timeProvider);
        return useCase.ExecuteAsync(id, new SetStockRequest(quantity), CancellationToken.None);
    }

    [Theory]
    [InlineData(40)]
    [InlineData(0)]
    public async Task Execute_ValidQuantity_SetsAbsoluteValue(int quantity)
    {
        var product = AddProduct(stock: 5);

        var result = await Run(product.Id, quantity);

        Assert.Equal(quantity, result.Stock);
        Assert.Equal(quantity, product.Stock);
        Assert.Equal(1, _unitOfWork.SaveCount);
    }

    [Fact]
    public async Task Execute_NegativeQuantity_ThrowsStockNegative()
    {
        var product = AddProduct();

        var exception = await Assert.ThrowsAsync<CatalogException>(() => Run(product.Id, -1));

        Assert.Equal(ErrorCodes.StockNegative, exception.Code);
        Assert.Equal(5, product.Stock);
        Assert.Equal(0, _unitOfWork.SaveCount);
    }

    [Fact]
    public async Task Execute_FractionalQuantity_ThrowsValidationErrorOnQuantity()
    {
        var product = AddProduct();

        var exception = await Assert.ThrowsAsync<CatalogException>(() => Run(product.Id, 4.5m));

        Assert.Equal(ErrorCodes.ValidationError, exception.Code);
        var error = Assert.Single(exception.Errors!);
        Assert.Equal("quantity", error.Field);
        Assert.Equal(ErrorCodes.InvalidFormat, error.Code);
        Assert.Equal(0, _unitOfWork.SaveCount);
    }

    [Fact]
    public async Task Execute_MissingQuantity_ThrowsRequiredOnQuantity()
    {
        var product = AddProduct();

        var exception = await Assert.ThrowsAsync<CatalogException>(() => Run(product.Id, null));

        Assert.Equal(ErrorCodes.ValidationError, exception.Code);
        Assert.Equal(ErrorCodes.Required, Assert.Single(exception.Errors!).Code);
    }

    [Fact]
    public async Task Execute_MissingProduct_ThrowsNotFound()
    {
        var exception = await Assert.ThrowsAsync<CatalogException>(() => Run(Guid.NewGuid(), 3));

        Assert.Equal(ErrorCodes.NotFound, exception.Code);
        Assert.Equal(0, _unitOfWork.SaveCount);
    }
}
