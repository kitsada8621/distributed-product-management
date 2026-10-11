using Catalog.Application.Categories;
using Catalog.Domain.Categories;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Infrastructure.Persistence;

public sealed class CategoryRepository(CatalogDbContext context) : ICategoryRepository
{
    public async Task<IReadOnlyList<Category>> ListActiveAsync(CancellationToken cancellationToken)
    {
        return await context.Categories
            .AsNoTracking()
            .Where(c => !c.IsDeleted)
            .OrderBy(c => c.Name)
            .ToListAsync(cancellationToken);
    }

    public Task<Category?> FindActiveAsync(Guid id, CancellationToken cancellationToken)
    {
        return context.Categories.FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted, cancellationToken);
    }

    // The name column is COLLATE NOCASE, so == is case-insensitive in SQL.
    public Task<bool> NameExistsAsync(string name, Guid? excludingId, CancellationToken cancellationToken)
    {
        return context.Categories.AnyAsync(
            c => !c.IsDeleted && c.Name == name && (excludingId == null || c.Id != excludingId),
            cancellationToken);
    }

    public async Task AddAsync(Category category, CancellationToken cancellationToken)
    {
        await context.Categories.AddAsync(category, cancellationToken);
    }
}
