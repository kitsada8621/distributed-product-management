using Catalog.Application.Common;
using Catalog.Domain.Categories;

namespace Catalog.Application.Categories;

public sealed record CreateCategoryRequest(string? Name);

public sealed class CreateCategory(
    ICategoryRepository categories,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
{
    public async Task<CategoryView> ExecuteAsync(CreateCategoryRequest request, CancellationToken cancellationToken)
    {
        var name = request.Name?.Trim() ?? string.Empty;
        if (name.Length == 0)
        {
            var errors = new[] { new FieldError("name", ErrorCodes.Required, "Name is required") };
            throw new CatalogException(ErrorCodes.ValidationError, "Invalid input", errors);
        }

        if (await categories.NameExistsAsync(name, null, cancellationToken))
        {
            throw new CatalogException(ErrorCodes.CategoryNameExists, "Category name already exists");
        }

        var category = Category.Create(name, timeProvider.GetUtcNow().UtcDateTime);
        await categories.AddAsync(category, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return CategoryView.From(category);
    }
}
