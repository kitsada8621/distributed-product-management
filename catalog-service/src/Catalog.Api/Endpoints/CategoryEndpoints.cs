using Catalog.Api.Common;
using Catalog.Application.Categories;

namespace Catalog.Api.Endpoints;

public static class CategoryEndpoints
{
    public static void MapCategoryEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/catalog/categories");

        group.MapGet("/", ListAsync).AllowAnonymous();
        group.MapPost("/", CreateAsync).RequireAuthorization(JwtBearerSetup.AdminPolicy);
        group.MapPut("/{id:guid}", UpdateAsync).RequireAuthorization(JwtBearerSetup.AdminPolicy);
        group.MapDelete("/{id:guid}", DeleteAsync).RequireAuthorization(JwtBearerSetup.AdminPolicy);
    }

    private static async Task<IResult> ListAsync(ListCategories listCategories, CancellationToken cancellationToken)
    {
        var categories = await listCategories.ExecuteAsync(cancellationToken);
        return ApiResponse.Ok(StatusCodes.Status200OK, "Categories retrieved", categories).ToResult();
    }

    private static async Task<IResult> CreateAsync(
        CreateCategoryRequest request, CreateCategory createCategory, CancellationToken cancellationToken)
    {
        var category = await createCategory.ExecuteAsync(request, cancellationToken);
        return ApiResponse.Ok(StatusCodes.Status201Created, "Category created", category).ToResult();
    }

    private static async Task<IResult> UpdateAsync(
        Guid id, UpdateCategoryRequest request, UpdateCategory updateCategory, CancellationToken cancellationToken)
    {
        var category = await updateCategory.ExecuteAsync(id, request, cancellationToken);
        return ApiResponse.Ok(StatusCodes.Status200OK, "Category updated", category).ToResult();
    }

    private static async Task<IResult> DeleteAsync(
        Guid id, DeleteCategory deleteCategory, CancellationToken cancellationToken)
    {
        await deleteCategory.ExecuteAsync(id, cancellationToken);
        return ApiResponse.Ok(StatusCodes.Status200OK, "Category deleted", null).ToResult();
    }
}
