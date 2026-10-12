using Catalog.Application.Categories;
using Catalog.Application.Common;
using Catalog.Domain.Categories;
using Catalog.UnitTests.Fakes;

namespace Catalog.UnitTests.Categories;

public class CreateCategoryTests
{
    private readonly FakeCategoryRepository _repository = new();
    private readonly FakeUnitOfWork _unitOfWork = new();
    private readonly FakeTimeProvider _timeProvider = new();

    private CreateCategory CreateUseCase()
    {
        return new CreateCategory(_repository, _unitOfWork, _timeProvider);
    }

    [Fact]
    public async Task Execute_ValidName_SavesOnceAndReturnsView()
    {
        var result = await CreateUseCase().ExecuteAsync(new CreateCategoryRequest("  Drinks  "), CancellationToken.None);

        Assert.Equal("Drinks", result.Name);
        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Single(_repository.Items);
        Assert.Equal(1, _unitOfWork.SaveCount);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Execute_EmptyName_ThrowsValidationErrorOnNameField(string? name)
    {
        var exception = await Assert.ThrowsAsync<CatalogException>(
            () => CreateUseCase().ExecuteAsync(new CreateCategoryRequest(name), CancellationToken.None));

        Assert.Equal(ErrorCodes.ValidationError, exception.Code);
        var error = Assert.Single(exception.Errors!);
        Assert.Equal("name", error.Field);
        Assert.Equal(ErrorCodes.Required, error.Code);
        Assert.Equal(0, _unitOfWork.SaveCount);
    }

    [Fact]
    public async Task Execute_NameDiffersOnlyByCase_ThrowsCategoryNameExists()
    {
        _repository.Items.Add(Category.Create("Drinks", _timeProvider.GetUtcNow().UtcDateTime));

        var exception = await Assert.ThrowsAsync<CatalogException>(
            () => CreateUseCase().ExecuteAsync(new CreateCategoryRequest("dRINKS"), CancellationToken.None));

        Assert.Equal(ErrorCodes.CategoryNameExists, exception.Code);
        Assert.Equal(0, _unitOfWork.SaveCount);
    }

    [Fact]
    public async Task Execute_SameNameAsDeletedCategory_IsAllowed()
    {
        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var deleted = Category.Create("Drinks", now);
        deleted.SoftDelete(now);
        _repository.Items.Add(deleted);

        var result = await CreateUseCase().ExecuteAsync(new CreateCategoryRequest("Drinks"), CancellationToken.None);

        Assert.Equal("Drinks", result.Name);
        Assert.Equal(2, _repository.Items.Count);
    }
}
