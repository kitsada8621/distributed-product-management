using Catalog.Application.Categories;
using Catalog.Domain.Categories;
using Catalog.UnitTests.Fakes;

namespace Catalog.UnitTests.Categories;

public class ListCategoriesTests
{
    private static readonly DateTime Now = new(2026, 10, 10, 8, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task Execute_ReturnsOnlyNonDeletedSortedByName()
    {
        var repository = new FakeCategoryRepository();
        var deleted = Category.Create("Archive", Now);
        deleted.SoftDelete(Now);
        repository.Items.Add(Category.Create("Snacks", Now));
        repository.Items.Add(deleted);
        repository.Items.Add(Category.Create("Drinks", Now));
        var useCase = new ListCategories(repository);

        var result = await useCase.ExecuteAsync(CancellationToken.None);

        Assert.Equal(["Drinks", "Snacks"], result.Select(view => view.Name).ToArray());
    }
}
