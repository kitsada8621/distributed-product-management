using Catalog.Domain.Categories;
using Catalog.Infrastructure.Persistence;

namespace Catalog.IntegrationTests.Persistence;

public sealed class CategoryRepositoryTests : IDisposable
{
    private static readonly DateTime Now = new(2026, 10, 10, 8, 30, 0, DateTimeKind.Utc);

    private readonly TestDatabase _database = new();

    public void Dispose()
    {
        _database.Dispose();
    }

    private async Task<Category> SeedAsync(string name, bool deleted = false)
    {
        await using var context = _database.CreateContext();
        var category = Category.Create(name, Now);
        if (deleted)
        {
            category.SoftDelete(Now);
        }
        context.Categories.Add(category);
        await context.SaveChangesAsync();
        return category;
    }

    [Fact]
    public async Task ListActive_ExcludesDeletedAndSortsByName()
    {
        await SeedAsync("snacks");
        await SeedAsync("Archive", deleted: true);
        await SeedAsync("Drinks");
        await using var context = _database.CreateContext();

        var result = await new CategoryRepository(context).ListActiveAsync(CancellationToken.None);

        Assert.Equal(["Drinks", "snacks"], result.Select(c => c.Name).ToArray());
    }

    [Fact]
    public async Task FindActive_ReturnsActiveCategory()
    {
        var seeded = await SeedAsync("Drinks");
        await using var context = _database.CreateContext();

        var found = await new CategoryRepository(context).FindActiveAsync(seeded.Id, CancellationToken.None);

        Assert.Equal("Drinks", found!.Name);
    }

    [Fact]
    public async Task FindActive_DeletedOrUnknown_ReturnsNull()
    {
        var deleted = await SeedAsync("Drinks", deleted: true);
        await using var context = _database.CreateContext();
        var repository = new CategoryRepository(context);

        Assert.Null(await repository.FindActiveAsync(deleted.Id, CancellationToken.None));
        Assert.Null(await repository.FindActiveAsync(Guid.NewGuid(), CancellationToken.None));
    }

    [Fact]
    public async Task NameExists_IsCaseInsensitive()
    {
        await SeedAsync("Drinks");
        await using var context = _database.CreateContext();

        var exists = await new CategoryRepository(context).NameExistsAsync("dRINKS", null, CancellationToken.None);

        Assert.True(exists);
    }

    [Fact]
    public async Task NameExists_IgnoresDeletedCategories()
    {
        await SeedAsync("Drinks", deleted: true);
        await using var context = _database.CreateContext();

        var exists = await new CategoryRepository(context).NameExistsAsync("Drinks", null, CancellationToken.None);

        Assert.False(exists);
    }

    [Fact]
    public async Task NameExists_HonoursExcludingId()
    {
        var seeded = await SeedAsync("Drinks");
        await using var context = _database.CreateContext();
        var repository = new CategoryRepository(context);

        Assert.False(await repository.NameExistsAsync("Drinks", seeded.Id, CancellationToken.None));
        Assert.True(await repository.NameExistsAsync("Drinks", Guid.NewGuid(), CancellationToken.None));
    }

    [Fact]
    public async Task Add_PersistsOnlyAfterSave()
    {
        var category = Category.Create("Drinks", Now);
        await using (var context = _database.CreateContext())
        {
            await new CategoryRepository(context).AddAsync(category, CancellationToken.None);
            await context.SaveChangesAsync();
        }

        await using var readContext = _database.CreateContext();
        var found = await new CategoryRepository(readContext).FindActiveAsync(category.Id, CancellationToken.None);
        Assert.NotNull(found);
    }
}
