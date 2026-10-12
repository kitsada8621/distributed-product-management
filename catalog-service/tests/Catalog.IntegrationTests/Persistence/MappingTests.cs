using System.Text.RegularExpressions;
using Catalog.Domain.Categories;
using Catalog.Domain.Products;
using Microsoft.EntityFrameworkCore;

namespace Catalog.IntegrationTests.Persistence;

public sealed class MappingTests : IDisposable
{
    private static readonly DateTime Now = new(2026, 10, 10, 8, 30, 0, DateTimeKind.Utc);

    private readonly TestDatabase _database = new();

    public void Dispose()
    {
        _database.Dispose();
    }

    [Theory]
    [InlineData("SELECT CAST(foreign_keys AS TEXT) AS Value FROM pragma_foreign_keys", "1")]
    [InlineData("SELECT journal_mode AS Value FROM pragma_journal_mode", "wal")]
    [InlineData("SELECT CAST(timeout AS TEXT) AS Value FROM pragma_busy_timeout", "5000")]
    public async Task NewConnection_HasPragma(string query, string expected)
    {
        await using var context = _database.CreateContext();

        var values = await context.Database.SqlQueryRaw<string>(query).ToListAsync();

        Assert.Equal(expected, Assert.Single(values));
    }

    [Fact]
    public async Task CategoryAndProduct_RoundTrip()
    {
        var category = Category.Create("Drinks", Now);
        var product = Product.Create("Tea", "Green tea", 12900, category.Id, 7, Now);
        await using (var context = _database.CreateContext())
        {
            context.Categories.Add(category);
            context.Products.Add(product);
            await context.SaveChangesAsync();
        }

        await using var readContext = _database.CreateContext();
        var savedCategory = await readContext.Categories.SingleAsync();
        var savedProduct = await readContext.Products.SingleAsync();

        Assert.Equal("Drinks", savedCategory.Name);
        Assert.Equal(Now, savedCategory.CreatedAt);
        Assert.Equal("Tea", savedProduct.Name);
        Assert.Equal("Green tea", savedProduct.Description);
        Assert.Equal(12900, savedProduct.PriceMinor);
        Assert.Equal(7, savedProduct.Stock);
        Assert.Equal(category.Id, savedProduct.CategoryId);
        Assert.False(savedProduct.IsDeleted);
    }

    [Fact]
    public async Task CreatedAt_IsStoredAsIsoUtcText()
    {
        await using var context = _database.CreateContext();
        context.Categories.Add(Category.Create("Drinks", Now));
        await context.SaveChangesAsync();

        var stored = await context.Database
            .SqlQueryRaw<string>("SELECT created_at AS Value FROM categories")
            .SingleAsync();

        Assert.Matches(new Regex(@"^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}Z$"), stored);
        Assert.Equal("2026-10-10T08:30:00Z", stored);
    }

    [Fact]
    public async Task ActiveCategories_NamesDifferingOnlyByCase_AreRejected()
    {
        await using var context = _database.CreateContext();
        context.Categories.Add(Category.Create("Drinks", Now));
        await context.SaveChangesAsync();

        context.Categories.Add(Category.Create("DRINKS", Now));

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    [Fact]
    public async Task DeletedCategoryName_CanBeUsedAgain()
    {
        await using var context = _database.CreateContext();
        var first = Category.Create("Drinks", Now);
        context.Categories.Add(first);
        await context.SaveChangesAsync();
        first.SoftDelete(Now);
        await context.SaveChangesAsync();

        context.Categories.Add(Category.Create("drinks", Now));
        await context.SaveChangesAsync();

        Assert.Equal(2, await context.Categories.CountAsync());
    }

    [Fact]
    public async Task Product_PriceZero_IsRejectedByCheckConstraint()
    {
        await using var context = _database.CreateContext();
        context.Products.Add(Product.Create("Tea", "", 0, null, 1, Now));

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    [Fact]
    public async Task Product_NegativeStock_IsRejectedByCheckConstraint()
    {
        await using var context = _database.CreateContext();
        context.Products.Add(Product.Create("Tea", "", 100, null, -1, Now));

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    [Fact]
    public async Task Product_MissingCategory_IsRejectedByForeignKey()
    {
        await using var context = _database.CreateContext();
        context.Products.Add(Product.Create("Tea", "", 100, Guid.NewGuid(), 1, Now));

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    [Fact]
    public async Task Product_NullCategory_IsAccepted()
    {
        await using var context = _database.CreateContext();
        context.Products.Add(Product.Create("Tea", "", 100, null, 1, Now));

        await context.SaveChangesAsync();

        Assert.Equal(1, await context.Products.CountAsync());
    }
}
