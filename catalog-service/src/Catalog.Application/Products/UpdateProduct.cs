using Catalog.Application.Categories;
using Catalog.Application.Common;

namespace Catalog.Application.Products;

public sealed record UpdateProductRequest(string? Name, string? Description, decimal? Price, Guid? CategoryId);

public sealed class UpdateProduct(
    IProductRepository products,
    ICategoryRepository categories,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
{
    public async Task<ProductView> ExecuteAsync(
        Guid id,
        UpdateProductRequest request,
        CancellationToken cancellationToken)
    {
        var product = await products.FindActiveAsync(id, cancellationToken);
        if (product is null)
        {
            throw new CatalogException(ErrorCodes.NotFound, "Product not found");
        }

        var errors = new List<FieldError>();
        var validated = await ProductInputValidator.ValidateAsync(
            request.Name, request.Description, request.Price, request.CategoryId, categories, errors, cancellationToken);
        if (errors.Count > 0)
        {
            throw new CatalogException(ErrorCodes.ValidationError, "Invalid input", errors);
        }

        product.Update(
            validated.Name,
            validated.Description,
            validated.PriceMinor,
            request.CategoryId,
            timeProvider.GetUtcNow().UtcDateTime);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return ProductView.From(product, validated.CategoryName, includeStock: true);
    }
}
