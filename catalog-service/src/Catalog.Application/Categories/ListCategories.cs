namespace Catalog.Application.Categories;

public sealed class ListCategories(ICategoryRepository categories)
{
    public async Task<IReadOnlyList<CategoryView>> ExecuteAsync(CancellationToken cancellationToken)
    {
        var active = await categories.ListActiveAsync(cancellationToken);
        return active.Select(CategoryView.From).ToList();
    }
}
