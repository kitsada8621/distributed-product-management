using Catalog.Application.Categories;
using Catalog.Application.Common;
using Catalog.Domain.Categories;
using Catalog.UnitTests.Fakes;

namespace Catalog.UnitTests.Categories;

public class UpdateCategoryTests
{
    private readonly FakeCategoryRepository _repository = new();
    private readonly FakeUnitOfWork _unitOfWork = new();
    private readonly FakeTimeProvider _timeProvider = new();

    private UpdateCategory CreateUseCase()
    {
        return new UpdateCategory(_repository, _unitOfWork, _timeProvider);
    }

    private Category AddCategory(string name)
    {
        var category = Category.Create(name, _timeProvider.GetUtcNow().UtcDateTime);
        _repository.Items.Add(category);
        return category;
    }

    [Fact]
    public async Task Execute_ValidName_RenamesAndSavesOnce()
    {
        var category = AddCategory("Drinks");

        var result = await CreateUseCase().ExecuteAsync(category.Id, new UpdateCategoryRequest(" Beverages "), CancellationToken.None);

        Assert.Equal("Beverages", result.Name);
        Assert.Equal("Beverages", category.Name);
        Assert.Equal(1, _unitOfWork.SaveCount);
    }

    [Fact]
    public async Task Execute_MissingId_ThrowsNotFound()
    {
        var exception = await Assert.ThrowsAsync<CatalogException>(
            () => CreateUseCase().ExecuteAsync(Guid.NewGuid(), new UpdateCategoryRequest("Drinks"), CancellationToken.None));

        Assert.Equal(ErrorCodes.NotFound, exception.Code);
    }

    [Fact]
    public async Task Execute_DeletedCategory_ThrowsNotFound()
    {
        var category = AddCategory("Drinks");
        category.SoftDelete(_timeProvider.GetUtcNow().UtcDateTime);

        var exception = await Assert.ThrowsAsync<CatalogException>(
            () => CreateUseCase().ExecuteAsync(category.Id, new UpdateCategoryRequest("Snacks"), CancellationToken.None));

        Assert.Equal(ErrorCodes.NotFound, exception.Code);
    }

    [Fact]
    public async Task Execute_EmptyName_ThrowsValidationErrorOnNameField()
    {
        var category = AddCategory("Drinks");

        var exception = await Assert.ThrowsAsync<CatalogException>(
            () => CreateUseCase().ExecuteAsync(category.Id, new UpdateCategoryRequest("  "), CancellationToken.None));

        Assert.Equal(ErrorCodes.ValidationError, exception.Code);
        Assert.Equal("name", Assert.Single(exception.Errors!).Field);
        Assert.Equal(0, _unitOfWork.SaveCount);
    }

    [Fact]
    public async Task Execute_NameOfAnotherCategory_ThrowsCategoryNameExists()
    {
        AddCategory("Snacks");
        var category = AddCategory("Drinks");

        var exception = await Assert.ThrowsAsync<CatalogException>(
            () => CreateUseCase().ExecuteAsync(category.Id, new UpdateCategoryRequest("snacks"), CancellationToken.None));

        Assert.Equal(ErrorCodes.CategoryNameExists, exception.Code);
        Assert.Equal("Drinks", category.Name);
    }

    [Fact]
    public async Task Execute_SameNameForItself_IsAllowed()
    {
        var category = AddCategory("Drinks");

        var result = await CreateUseCase().ExecuteAsync(category.Id, new UpdateCategoryRequest("DRINKS"), CancellationToken.None);

        Assert.Equal("DRINKS", result.Name);
    }
}
