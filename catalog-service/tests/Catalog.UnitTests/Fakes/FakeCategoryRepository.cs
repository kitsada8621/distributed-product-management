using Catalog.Application.Categories;
using Catalog.Domain.Categories;

namespace Catalog.UnitTests.Fakes;

public sealed class FakeCategoryRepository : ICategoryRepository
{
    public List<Category> Items { get; } = [];

    public Task<IReadOnlyList<Category>> ListActiveAsync(CancellationToken cancellationToken)
    {
        IReadOnlyList<Category> active = Items
            .Where(category => !category.IsDeleted)
            .OrderBy(category => category.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
        return Task.FromResult(active);
    }

    public Task<Category?> FindActiveAsync(Guid id, CancellationToken cancellationToken)
    {
        var found = Items.FirstOrDefault(category => category.Id == id && !category.IsDeleted);
        return Task.FromResult(found);
    }

    public Task<bool> NameExistsAsync(string name, Guid? excludingId, CancellationToken cancellationToken)
    {
        var exists = Items.Any(category =>
            !category.IsDeleted
            && category.Id != excludingId
            && string.Equals(category.Name, name, StringComparison.OrdinalIgnoreCase));
        return Task.FromResult(exists);
    }

    public Task AddAsync(Category category, CancellationToken cancellationToken)
    {
        Items.Add(category);
        return Task.CompletedTask;
    }
}
