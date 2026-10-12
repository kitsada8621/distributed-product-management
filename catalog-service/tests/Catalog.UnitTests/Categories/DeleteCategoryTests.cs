using Catalog.Application.Categories;
using Catalog.Application.Common;
using Catalog.Domain.Categories;
using Catalog.Domain.Products;
using Catalog.UnitTests.Fakes;

namespace Catalog.UnitTests.Categories;

public class DeleteCategoryTests
{
    private readonly FakeCategoryRepository _categories = new();
    private readonly FakeProductRepository _products = new();
    private readonly FakeUnitOfWork _unitOfWork = new();
    private readonly FakeTimeProvider _timeProvider = new();

    private DeleteCategory CreateUseCase()
    {
        return new DeleteCategory(_categories, _products, _unitOfWork, _timeProvider);
    }

    private DateTime Now => _timeProvider.GetUtcNow().UtcDateTime;

    [Fact]
    public async Task Execute_SoftDeletesCategoryAndClearsItsProducts()
    {
        var category = Category.Create("Drinks", Now);
        var other = Category.Create("Snacks", Now);
        _categories.Items.AddRange([category, other]);
        var tea = Product.Create("Tea", "", 100, category.Id, 5, Now);
        var oldCoffee = Product.Create("Coffee", "", 100, category.Id, 5, Now);
        oldCoffee.SoftDelete(Now);
        var chips = Product.Create("Chips", "", 100, other.Id, 5, Now);
        _products.Items.AddRange([tea, oldCoffee, chips]);

        await CreateUseCase().ExecuteAsync(category.Id, CancellationToken.None);

        Assert.True(category.IsDeleted);
        Assert.Null(tea.CategoryId);
        Assert.Null(oldCoffee.CategoryId);
        Assert.Equal(other.Id, chips.CategoryId);
        Assert.Equal(1, _unitOfWork.SaveCount);
    }

    [Fact]
    public async Task Execute_CategoryWithoutProducts_StillSucceeds()
    {
        var category = Category.Create("Drinks", Now);
        _categories.Items.Add(category);

        await CreateUseCase().ExecuteAsync(category.Id, CancellationToken.None);

        Assert.True(category.IsDeleted);
        Assert.Equal(1, _unitOfWork.SaveCount);
    }

    [Fact]
    public async Task Execute_MissingId_ThrowsNotFound()
    {
        var exception = await Assert.ThrowsAsync<CatalogException>(
            () => CreateUseCase().ExecuteAsync(Guid.NewGuid(), CancellationToken.None));

        Assert.Equal(ErrorCodes.NotFound, exception.Code);
        Assert.Equal(0, _unitOfWork.SaveCount);
    }

    [Fact]
    public async Task Execute_AlreadyDeleted_ThrowsNotFound()
    {
        var category = Category.Create("Drinks", Now);
        category.SoftDelete(Now);
        _categories.Items.Add(category);

        var exception = await Assert.ThrowsAsync<CatalogException>(
            () => CreateUseCase().ExecuteAsync(category.Id, CancellationToken.None));

        Assert.Equal(ErrorCodes.NotFound, exception.Code);
    }
}
