using Catalog.Api.Common;
using Catalog.Application.Products;

namespace Catalog.Api.Endpoints;

public static class ProductEndpoints
{
    public static void MapProductEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/catalog/products");

        group.MapGet("/", ListAsync).AllowAnonymous();
        group.MapGet("/{id:guid}", GetAsync).AllowAnonymous();
        group.MapPost("/", CreateAsync).RequireAuthorization(JwtBearerSetup.AdminPolicy);
        group.MapPut("/{id:guid}", UpdateAsync).RequireAuthorization(JwtBearerSetup.AdminPolicy);
        group.MapDelete("/{id:guid}", DeleteAsync).RequireAuthorization(JwtBearerSetup.AdminPolicy);
        group.MapPut("/{id:guid}/stock", SetStockAsync).RequireAuthorization(JwtBearerSetup.AdminPolicy);
    }

    // Query values stay strings so a bad value becomes 422 from the use case, not 400 from binding.
    private static async Task<IResult> ListAsync(
        string? q,
        string? categoryId,
        string? sort,
        string? page,
        string? pageSize,
        HttpContext context,
        ListProducts listProducts,
        CancellationToken cancellationToken)
    {
        var request = new ListProductsRequest(q, categoryId, sort, page, pageSize, IsAdmin(context));
        var result = await listProducts.ExecuteAsync(request, cancellationToken);

        var meta = new { result.Page, result.PageSize, result.Total, result.TotalPages };
        return ApiResponse.Ok(StatusCodes.Status200OK, "Products retrieved", result.Items, meta).ToResult();
    }

    private static async Task<IResult> GetAsync(
        Guid id, HttpContext context, GetProduct getProduct, CancellationToken cancellationToken)
    {
        var product = await getProduct.ExecuteAsync(id, IsAdmin(context), cancellationToken);
        return ApiResponse.Ok(StatusCodes.Status200OK, "Product retrieved", product).ToResult();
    }

    private static async Task<IResult> CreateAsync(
        CreateProductRequest request, CreateProduct createProduct, CancellationToken cancellationToken)
    {
        var product = await createProduct.ExecuteAsync(request, cancellationToken);
        return ApiResponse.Ok(StatusCodes.Status201Created, "Product created", product).ToResult();
    }

    private static async Task<IResult> UpdateAsync(
        Guid id, UpdateProductRequest request, UpdateProduct updateProduct, CancellationToken cancellationToken)
    {
        var product = await updateProduct.ExecuteAsync(id, request, cancellationToken);
        return ApiResponse.Ok(StatusCodes.Status200OK, "Product updated", product).ToResult();
    }

    private static async Task<IResult> DeleteAsync(
        Guid id, DeleteProduct deleteProduct, CancellationToken cancellationToken)
    {
        await deleteProduct.ExecuteAsync(id, cancellationToken);
        return ApiResponse.Ok(StatusCodes.Status200OK, "Product deleted", null).ToResult();
    }

    private static async Task<IResult> SetStockAsync(
        Guid id, SetStockRequest request, SetProductStock setProductStock, CancellationToken cancellationToken)
    {
        var result = await setProductStock.ExecuteAsync(id, request, cancellationToken);
        return ApiResponse.Ok(StatusCodes.Status200OK, "Stock updated", result).ToResult();
    }

    // An invalid or missing token leaves the caller a guest, so this is false rather than a 401.
    private static bool IsAdmin(HttpContext context)
    {
        return context.User.IsInRole("admin");
    }
}
