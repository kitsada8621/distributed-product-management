using Catalog.Domain.Categories;

namespace Catalog.Application.Categories;

public sealed record CategoryView(Guid Id, string Name)
{
    public static CategoryView From(Category category)
    {
        return new CategoryView(category.Id, category.Name);
    }
}
