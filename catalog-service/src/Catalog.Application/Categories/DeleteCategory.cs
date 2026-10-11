using Catalog.Application.Common;
using Catalog.Application.Products;

namespace Catalog.Application.Categories;

public sealed class DeleteCategory(
    ICategoryRepository categories,
    IProductRepository products,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
{
    public async Task ExecuteAsync(Guid id, CancellationToken cancellationToken)
    {
        var category = await categories.FindActiveAsync(id, cancellationToken);
        if (category is null)
        {
            throw new CatalogException(ErrorCodes.NotFound, "Category not found");
        }

        var now = timeProvider.GetUtcNow().UtcDateTime;
        category.SoftDelete(now);

        var categoryProducts = await products.ListByCategoryAsync(id, cancellationToken);
        foreach (var product in categoryProducts)
        {
            product.ClearCategory(now);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
