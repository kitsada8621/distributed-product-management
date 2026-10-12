using Catalog.Application.Common;
using Catalog.Application.Products;
using Catalog.Domain.Products;
using Catalog.UnitTests.Fakes;

namespace Catalog.UnitTests.Products;

public class DeleteProductTests
{
    private readonly FakeProductRepository _products = new();
    private readonly FakeUnitOfWork _unitOfWork = new();
    private readonly FakeTimeProvider _timeProvider = new();

    private DeleteProduct CreateUseCase()
    {
        return new DeleteProduct(_products, _unitOfWork, _timeProvider);
    }

    [Fact]
    public async Task Execute_MarksDeletedAndSavesOnce()
    {
        var product = Product.Create("Tea", "", 100, null, 5, _timeProvider.GetUtcNow().UtcDateTime);
        _products.Items.Add(product);

        await CreateUseCase().ExecuteAsync(product.Id, CancellationToken.None);

        Assert.True(product.IsDeleted);
        Assert.Equal(1, _unitOfWork.SaveCount);
    }

    [Fact]
    public async Task Execute_MissingProduct_ThrowsNotFound()
    {
        var exception = await Assert.ThrowsAsync<CatalogException>(
            () => CreateUseCase().ExecuteAsync(Guid.NewGuid(), CancellationToken.None));

        Assert.Equal(ErrorCodes.NotFound, exception.Code);
        Assert.Equal(0, _unitOfWork.SaveCount);
    }

    [Fact]
    public async Task Execute_AlreadyDeleted_ThrowsNotFound()
    {
        var product = Product.Create("Tea", "", 100, null, 5, _timeProvider.GetUtcNow().UtcDateTime);
        product.SoftDelete(_timeProvider.GetUtcNow().UtcDateTime);
        _products.Items.Add(product);

        var exception = await Assert.ThrowsAsync<CatalogException>(
            () => CreateUseCase().ExecuteAsync(product.Id, CancellationToken.None));

        Assert.Equal(ErrorCodes.NotFound, exception.Code);
    }
}
