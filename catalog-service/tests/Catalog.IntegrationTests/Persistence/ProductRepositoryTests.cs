using Catalog.Application.Products;
using Catalog.Domain.Categories;
using Catalog.Domain.Products;
using Catalog.Infrastructure.Persistence;

namespace Catalog.IntegrationTests.Persistence;

public sealed class ProductRepositoryTests : IDisposable
{
    private static readonly DateTime Now = new(2026, 10, 10, 8, 30, 0, DateTimeKind.Utc);

    private readonly TestDatabase _database = new();

    public void Dispose()
    {
        _database.Dispose();
    }

    private async Task<Product> SeedAsync(
        string name,
        long priceMinor = 100,
        Guid? categoryId = null,
        int minutesOld = 0,
        bool deleted = false)
    {
        await using var context = _database.CreateContext();
        var product = Product.Create(name, "", priceMinor, categoryId, 5, Now.AddMinutes(-minutesOld));
        if (deleted)
        {
            product.SoftDelete(Now);
        }
        context.Products.Add(product);
        await context.SaveChangesAsync();
        return product;
    }

    private async Task<Category> SeedCategoryAsync(string name, bool deleted = false)
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

    private async Task<ProductPage> SearchAsync(
        string? query = null,
        Guid? categoryId = null,
        ProductSort sort = ProductSort.NameAsc,
        int page = 1,
        int pageSize = 20)
    {
        await using var context = _database.CreateContext();
        var search = new ProductSearch(query, categoryId, sort, page, pageSize);
        return await new ProductRepository(context).SearchAsync(search, CancellationToken.None);
    }

    private static string[] Names(ProductPage page)
    {
        return page.Items.Select(item => item.Product.Name).ToArray();
    }

    [Fact]
    public async Task Search_ExcludesSoftDeletedProducts()
    {
        await SeedAsync("Tea");
        await SeedAsync("Old", deleted: true);

        var result = await SearchAsync();

        Assert.Equal(["Tea"], Names(result));
        Assert.Equal(1, result.Total);
    }

    [Fact]
    public async Task Search_Query_MatchesSubstringCaseInsensitively()
    {
        await SeedAsync("Green Tea");
        await SeedAsync("Coffee");

        var result = await SearchAsync(query: "TEA");

        Assert.Equal(["Green Tea"], Names(result));
    }

    [Fact]
    public async Task Search_Query_TreatsPercentAndUnderscoreLiterally()
    {
        await SeedAsync("100% Juice");
        await SeedAsync("1000 Juice");
        await SeedAsync("a_b");
        await SeedAsync("axb");

        Assert.Equal(["100% Juice"], Names(await SearchAsync(query: "0%")));
        Assert.Equal(["a_b"], Names(await SearchAsync(query: "a_b")));
    }

    [Fact]
    public async Task Search_CategoryFilter_ReturnsOnlyThatCategory()
    {
        var drinks = await SeedCategoryAsync("Drinks");
        await SeedAsync("Tea", categoryId: drinks.Id);
        await SeedAsync("Chips");

        var result = await SearchAsync(categoryId: drinks.Id);

        Assert.Equal(["Tea"], Names(result));
    }

    [Fact]
    public async Task Search_NameAsc_IsCaseInsensitive()
    {
        await SeedAsync("banana");
        await SeedAsync("Cherry");
        await SeedAsync("Apple");

        var result = await SearchAsync(sort: ProductSort.NameAsc);

        Assert.Equal(["Apple", "banana", "Cherry"], Names(result));
    }

    [Fact]
    public async Task Search_PriceSorts_OrderByPrice()
    {
        await SeedAsync("Mid", priceMinor: 500);
        await SeedAsync("Low", priceMinor: 100);
        await SeedAsync("High", priceMinor: 900);

        Assert.Equal(["Low", "Mid", "High"], Names(await SearchAsync(sort: ProductSort.PriceAsc)));
        Assert.Equal(["High", "Mid", "Low"], Names(await SearchAsync(sort: ProductSort.PriceDesc)));
    }

    [Fact]
    public async Task Search_Newest_OrdersByCreatedAtDescending()
    {
        await SeedAsync("Oldest", minutesOld: 30);
        await SeedAsync("Newest", minutesOld: 0);
        await SeedAsync("Middle", minutesOld: 10);

        var result = await SearchAsync(sort: ProductSort.Newest);

        Assert.Equal(["Newest", "Middle", "Oldest"], Names(result));
    }

    [Fact]
    public async Task Search_Paging_ReturnsRequestedPageAndTotal()
    {
        foreach (var name in new[] { "A", "B", "C", "D", "E" })
        {
            await SeedAsync(name);
        }

        var second = await SearchAsync(page: 2, pageSize: 2);
        var last = await SearchAsync(page: 3, pageSize: 2);

        Assert.Equal(["C", "D"], Names(second));
        Assert.Equal(5, second.Total);
        Assert.Equal(["E"], Names(last));
    }

    [Fact]
    public async Task Search_PageBeyondRange_ReturnsNoItemsButTotal()
    {
        await SeedAsync("A");
        await SeedAsync("B");

        var result = await SearchAsync(page: 9, pageSize: 2);

        Assert.Empty(result.Items);
        Assert.Equal(2, result.Total);
    }

    [Fact]
    public async Task Search_CategoryName_IsFilledOnlyForActiveCategory()
    {
        var drinks = await SeedCategoryAsync("Drinks");
        var archive = await SeedCategoryAsync("Archive", deleted: true);
        await SeedAsync("Tea", categoryId: drinks.Id);
        await SeedAsync("Plain");
        await SeedAsync("Orphan", categoryId: archive.Id);

        var result = await SearchAsync();

        var byName = result.Items.ToDictionary(item => item.Product.Name, item => item.CategoryName);
        Assert.Equal("Drinks", byName["Tea"]);
        Assert.Null(byName["Plain"]);
        Assert.Null(byName["Orphan"]);
    }

    [Fact]
    public async Task ListByCategory_IncludesSoftDeletedProducts()
    {
        var drinks = await SeedCategoryAsync("Drinks");
        await SeedAsync("Tea", categoryId: drinks.Id);
        await SeedAsync("Gone", categoryId: drinks.Id, deleted: true);
        await SeedAsync("Other");
        await using var context = _database.CreateContext();

        var result = await new ProductRepository(context).ListByCategoryAsync(drinks.Id, CancellationToken.None);

        Assert.Equal(["Gone", "Tea"], result.Select(p => p.Name).Order().ToArray());
    }

    [Fact]
    public async Task FindActive_DeletedOrUnknown_ReturnsNull()
    {
        var deleted = await SeedAsync("Gone", deleted: true);
        var active = await SeedAsync("Tea");
        await using var context = _database.CreateContext();
        var repository = new ProductRepository(context);

        Assert.Null(await repository.FindActiveAsync(deleted.Id, CancellationToken.None));
        Assert.Null(await repository.FindActiveAsync(Guid.NewGuid(), CancellationToken.None));
        Assert.Equal("Tea", (await repository.FindActiveAsync(active.Id, CancellationToken.None))!.Name);
    }

    [Fact]
    public async Task Add_PersistsAfterSave()
    {
        var product = Product.Create("Tea", "", 100, null, 3, Now);
        await using (var context = _database.CreateContext())
        {
            await new ProductRepository(context).AddAsync(product, CancellationToken.None);
            await context.SaveChangesAsync();
        }

        await using var readContext = _database.CreateContext();
        var found = await new ProductRepository(readContext).FindActiveAsync(product.Id, CancellationToken.None);
        Assert.Equal(3, found!.Stock);
    }
}
