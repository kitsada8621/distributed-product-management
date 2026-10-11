using Catalog.Application.Common;

namespace Catalog.Application.Products;

public sealed record SetStockRequest(decimal? Quantity);

public sealed record SetStockResult(int Stock);

public sealed class SetProductStock(IProductRepository products, IUnitOfWork unitOfWork, TimeProvider timeProvider)
{
    public async Task<SetStockResult> ExecuteAsync(
        Guid id,
        SetStockRequest request,
        CancellationToken cancellationToken)
    {
        var product = await products.FindActiveAsync(id, cancellationToken);
        if (product is null)
        {
            throw new CatalogException(ErrorCodes.NotFound, "Product not found");
        }

        var quantity = ValidateQuantity(request.Quantity);

        product.SetStock(quantity, timeProvider.GetUtcNow().UtcDateTime);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new SetStockResult(product.Stock);
    }

    private static int ValidateQuantity(decimal? quantity)
    {
        if (quantity is null)
        {
            throw ValidationFailure(ErrorCodes.Required, "Quantity is required");
        }

        if (quantity.Value != decimal.Truncate(quantity.Value) || quantity.Value > int.MaxValue)
        {
            throw ValidationFailure(ErrorCodes.InvalidFormat, "Quantity must be an integer");
        }

        if (quantity.Value < 0)
        {
            throw new CatalogException(ErrorCodes.StockNegative, "Stock cannot be negative");
        }

        return (int)quantity.Value;
    }

    private static CatalogException ValidationFailure(string fieldCode, string message)
    {
        var errors = new[] { new FieldError("quantity", fieldCode, message) };
        return new CatalogException(ErrorCodes.ValidationError, "Invalid input", errors);
    }
}
