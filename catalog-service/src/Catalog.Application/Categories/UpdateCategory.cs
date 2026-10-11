using Catalog.Application.Common;

namespace Catalog.Application.Categories;

public sealed record UpdateCategoryRequest(string? Name);

public sealed class UpdateCategory(
    ICategoryRepository categories,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
{
    public async Task<CategoryView> ExecuteAsync(
        Guid id,
        UpdateCategoryRequest request,
        CancellationToken cancellationToken)
    {
        var category = await categories.FindActiveAsync(id, cancellationToken);
        if (category is null)
        {
            throw new CatalogException(ErrorCodes.NotFound, "Category not found");
        }

        var name = request.Name?.Trim() ?? string.Empty;
        if (name.Length == 0)
        {
            var errors = new[] { new FieldError("name", ErrorCodes.Required, "Name is required") };
            throw new CatalogException(ErrorCodes.ValidationError, "Invalid input", errors);
        }

        if (await categories.NameExistsAsync(name, id, cancellationToken))
        {
            throw new CatalogException(ErrorCodes.CategoryNameExists, "Category name already exists");
        }

        category.Rename(name, timeProvider.GetUtcNow().UtcDateTime);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return CategoryView.From(category);
    }
}
