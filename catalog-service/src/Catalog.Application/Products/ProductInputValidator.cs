using Catalog.Application.Categories;
using Catalog.Application.Common;

namespace Catalog.Application.Products;

public sealed record ValidatedProduct(string Name, string Description, long PriceMinor, string? CategoryName);

public static class ProductInputValidator
{
    // Only appends to the caller's error list; the caller throws one ValidationError when it is not empty.
    public static async Task<ValidatedProduct> ValidateAsync(
        string? name,
        string? description,
        decimal? price,
        Guid? categoryId,
        ICategoryRepository categories,
        List<FieldError> errors,
        CancellationToken cancellationToken)
    {
        var trimmedName = name?.Trim() ?? string.Empty;
        if (trimmedName.Length == 0)
        {
            errors.Add(new FieldError("name", ErrorCodes.Required, "Name is required"));
        }

        var priceMinor = ValidatePrice(price, errors);
        var categoryName = await ValidateCategoryAsync(categoryId, categories, errors, cancellationToken);

        return new ValidatedProduct(trimmedName, description ?? string.Empty, priceMinor, categoryName);
    }

    private static long ValidatePrice(decimal? price, List<FieldError> errors)
    {
        if (price is null)
        {
            errors.Add(new FieldError("price", ErrorCodes.Required, "Price is required"));
            return 0;
        }

        if (!PriceConverter.TryToMinor(price.Value, out var priceMinor))
        {
            errors.Add(new FieldError(
                "price",
                ErrorCodes.OutOfRange,
                "Price must be greater than 0 with at most 2 decimals"));
            return 0;
        }

        return priceMinor;
    }

    private static async Task<string?> ValidateCategoryAsync(
        Guid? categoryId,
        ICategoryRepository categories,
        List<FieldError> errors,
        CancellationToken cancellationToken)
    {
        if (categoryId is null)
        {
            return null;
        }

        var category = await categories.FindActiveAsync(categoryId.Value, cancellationToken);
        if (category is null)
        {
            errors.Add(new FieldError("categoryId", ErrorCodes.InvalidFormat, "Category does not exist"));
            return null;
        }

        return category.Name;
    }
}
