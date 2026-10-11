using Catalog.Domain.Categories;

namespace Catalog.Application.Categories;

public interface ICategoryRepository
{
    Task<IReadOnlyList<Category>> ListActiveAsync(CancellationToken cancellationToken);

    Task<Category?> FindActiveAsync(Guid id, CancellationToken cancellationToken);

    Task<bool> NameExistsAsync(string name, Guid? excludingId, CancellationToken cancellationToken);

    Task AddAsync(Category category, CancellationToken cancellationToken);
}
