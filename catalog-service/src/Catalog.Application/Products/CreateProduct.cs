using Catalog.Application.Categories;
using Catalog.Application.Common;
using Catalog.Domain.Products;

namespace Catalog.Application.Products;

public sealed record CreateProductRequest(
    string? Name,
    string? Description,
    decimal? Price,
    Guid? CategoryId,
    decimal? Stock);

public sealed class CreateProduct(
    IProductRepository products,
    ICategoryRepository categories,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
{
    public async Task<ProductView> ExecuteAsync(CreateProductRequest request, CancellationToken cancellationToken)
    {
        var errors = new List<FieldError>();
        var validated = await ProductInputValidator.ValidateAsync(
            request.Name, request.Description, request.Price, request.CategoryId, categories, errors, cancellationToken);
        var stock = ValidateStock(request.Stock, errors);

        if (errors.Count > 0)
        {
            throw new CatalogException(ErrorCodes.ValidationError, "Invalid input", errors);
        }

        var product = Product.Create(
            validated.Name,
            validated.Description,
            validated.PriceMinor,
            request.CategoryId,
            stock,
            timeProvider.GetUtcNow().UtcDateTime);
        await products.AddAsync(product, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return ProductView.From(product, validated.CategoryName, includeStock: true);
    }

    private static int ValidateStock(decimal? stock, List<FieldError> errors)
    {
        if (stock is null)
        {
            errors.Add(new FieldError("stock", ErrorCodes.Required, "Stock is required"));
            return 0;
        }

        if (stock.Value != decimal.Truncate(stock.Value))
        {
            errors.Add(new FieldError("stock", ErrorCodes.InvalidFormat, "Stock must be an integer"));
            return 0;
        }

        if (stock.Value < 0 || stock.Value > int.MaxValue)
        {
            errors.Add(new FieldError("stock", ErrorCodes.OutOfRange, "Stock must be 0 or greater"));
            return 0;
        }

        return (int)stock.Value;
    }
}
