using Catalog.Application.Common;

namespace Catalog.Application.Products;

public sealed class DeleteProduct(IProductRepository products, IUnitOfWork unitOfWork, TimeProvider timeProvider)
{
    public async Task ExecuteAsync(Guid id, CancellationToken cancellationToken)
    {
        var product = await products.FindActiveAsync(id, cancellationToken);
        if (product is null)
        {
            throw new CatalogException(ErrorCodes.NotFound, "Product not found");
        }

        product.SoftDelete(timeProvider.GetUtcNow().UtcDateTime);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
