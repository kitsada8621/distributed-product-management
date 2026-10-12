using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Catalog.Domain.Categories;
using Catalog.Domain.Products;
using Catalog.Infrastructure.Persistence;
using Catalog.IntegrationTests.Support;
using Microsoft.EntityFrameworkCore;

namespace Catalog.IntegrationTests.Endpoints;

public class ProductReadEndpointTests(CatalogApiFactory factory) : IClassFixture<CatalogApiFactory>
{
    private const string Path = "/api/catalog/products";

    private static readonly DateTime Now = new(2026, 10, 10, 8, 30, 0, DateTimeKind.Utc);

    private HttpClient ClientWithToken(string? token)
    {
        var client = factory.CreateClient();
        if (token is not null)
        {
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }
        return client;
    }

    private static Task<JsonElement> ReadEnvelopeAsync(HttpResponseMessage response)
    {
        return response.Content.ReadFromJsonAsync<JsonElement>();
    }

    private CatalogDbContext CreateContext()
    {
        // Touching Server starts the host, which creates and migrates the database.
        _ = factory.Server;
        var options = new DbContextOptionsBuilder<CatalogDbContext>()
            .UseSqlite(factory.ConnectionString)
            .AddInterceptors(new SqlitePragmaInterceptor())
            .Options;
        return new CatalogDbContext(options);
    }

    // Every test uses its own unique name prefix so the shared database does not leak between tests.
    private static string Unique(string prefix) => $"{prefix}-{Guid.NewGuid():N}";

    private async Task<Product> SeedProductAsync(
        string name,
        long priceMinor = 129000,
        int stock = 5,
        Guid? categoryId = null,
        bool deleted = false,
        int minutesOld = 0)
    {
        await using var context = CreateContext();
        var product = Product.Create(name, "Desc", priceMinor, categoryId, stock, Now.AddMinutes(-minutesOld));
        if (deleted)
        {
            product.SoftDelete(Now);
        }
        context.Products.Add(product);
        await context.SaveChangesAsync();
        return product;
    }

    private async Task<Category> SeedCategoryAsync(string name)
    {
        await using var context = CreateContext();
        var category = Category.Create(name, Now);
        context.Categories.Add(category);
        await context.SaveChangesAsync();
        return category;
    }

    private async Task SoftDeleteCategoryAsync(Guid id)
    {
        await using var context = CreateContext();
        var category = await context.Categories.SingleAsync(c => c.Id == id);
        category.SoftDelete(Now);
        await context.SaveChangesAsync();
    }

    private async Task<JsonElement> ListAsync(string query, string? token = null)
    {
        var response = await ClientWithToken(token).GetAsync($"{Path}?{query}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await ReadEnvelopeAsync(response);
    }

    private static string[] Names(JsonElement envelope)
    {
        return envelope.GetProperty("data").EnumerateArray().Select(p => p.GetProperty("name").GetString()!).ToArray();
    }

    [Fact]
    public async Task List_AsGuest_ExcludesDeletedAndHidesStock()
    {
        var prefix = Unique("guest");
        await SeedProductAsync($"{prefix}-live", stock: 7);
        await SeedProductAsync($"{prefix}-gone", deleted: true);
        await SeedProductAsync($"{prefix}-empty", stock: 0);

        var body = await ListAsync($"q={prefix}&sort=name_asc");

        Assert.Equal([$"{prefix}-empty", $"{prefix}-live"], Names(body));
        var live = body.GetProperty("data")[1];
        Assert.Equal(JsonValueKind.Null, live.GetProperty("stock").ValueKind);
        Assert.True(live.GetProperty("inStock").GetBoolean());
        Assert.False(body.GetProperty("data")[0].GetProperty("inStock").GetBoolean());
    }

    [Fact]
    public async Task List_SerialisesPriceAsDecimalWithTwoDecimals()
    {
        var prefix = Unique("price");
        await SeedProductAsync($"{prefix}-a", priceMinor: 129000);

        var response = await factory.CreateClient().GetAsync($"{Path}?q={prefix}");

        var raw = await response.Content.ReadAsStringAsync();
        Assert.Contains("\"price\":1290.00", raw);
    }

    [Fact]
    public async Task List_MetaCarriesPagingFields()
    {
        var prefix = Unique("meta");
        for (var i = 0; i < 3; i++)
        {
            await SeedProductAsync($"{prefix}-{i}");
        }

        var body = await ListAsync($"q={prefix}&page=2&pageSize=2");

        var meta = body.GetProperty("meta");
        Assert.Equal(2, meta.GetProperty("page").GetInt32());
        Assert.Equal(2, meta.GetProperty("pageSize").GetInt32());
        Assert.Equal(3, meta.GetProperty("total").GetInt64());
        Assert.Equal(2, meta.GetProperty("totalPages").GetInt32());
        Assert.Single(body.GetProperty("data").EnumerateArray());
    }

    [Fact]
    public async Task List_AsAdmin_ShowsNumericStock()
    {
        var prefix = Unique("admin");
        await SeedProductAsync($"{prefix}-a", stock: 9);

        var body = await ListAsync($"q={prefix}", factory.CreateToken("admin"));

        Assert.Equal(9, body.GetProperty("data")[0].GetProperty("stock").GetInt32());
    }

    [Fact]
    public async Task List_AsUser_HidesStock()
    {
        var prefix = Unique("user");
        await SeedProductAsync($"{prefix}-a", stock: 9);

        var body = await ListAsync($"q={prefix}", factory.CreateToken("user"));

        Assert.Equal(JsonValueKind.Null, body.GetProperty("data")[0].GetProperty("stock").ValueKind);
    }

    [Fact]
    public async Task List_InvalidToken_IsStillOkAsGuest()
    {
        var prefix = Unique("badtoken");
        await SeedProductAsync($"{prefix}-a", stock: 9);

        var body = await ListAsync($"q={prefix}", "not.a.jwt");

        Assert.Equal(JsonValueKind.Null, body.GetProperty("data")[0].GetProperty("stock").ValueKind);
    }

    [Fact]
    public async Task List_CategoryFilter_ReturnsOnlyThatCategory()
    {
        var prefix = Unique("cat");
        var category = await SeedCategoryAsync(Unique("Drinks"));
        await SeedProductAsync($"{prefix}-in", categoryId: category.Id);
        await SeedProductAsync($"{prefix}-out");

        var body = await ListAsync($"q={prefix}&categoryId={category.Id}");

        Assert.Equal([$"{prefix}-in"], Names(body));
        Assert.Equal(category.Name, body.GetProperty("data")[0].GetProperty("categoryName").GetString());
    }

    [Fact]
    public async Task List_Sorts_OrderTheResult()
    {
        var prefix = Unique("sort");
        await SeedProductAsync($"{prefix}-b", priceMinor: 500, minutesOld: 20);
        await SeedProductAsync($"{prefix}-a", priceMinor: 900, minutesOld: 10);
        await SeedProductAsync($"{prefix}-c", priceMinor: 100, minutesOld: 0);

        Assert.Equal([$"{prefix}-a", $"{prefix}-b", $"{prefix}-c"], Names(await ListAsync($"q={prefix}&sort=name_asc")));
        Assert.Equal([$"{prefix}-c", $"{prefix}-b", $"{prefix}-a"], Names(await ListAsync($"q={prefix}&sort=price_asc")));
        Assert.Equal([$"{prefix}-a", $"{prefix}-b", $"{prefix}-c"], Names(await ListAsync($"q={prefix}&sort=price_desc")));
        Assert.Equal([$"{prefix}-c", $"{prefix}-a", $"{prefix}-b"], Names(await ListAsync($"q={prefix}&sort=newest")));
    }

    [Fact]
    public async Task List_PageBeyondRange_ReturnsEmptyData()
    {
        var prefix = Unique("beyond");
        await SeedProductAsync($"{prefix}-a");

        var body = await ListAsync($"q={prefix}&page=50");

        Assert.Empty(body.GetProperty("data").EnumerateArray());
        Assert.Equal(1, body.GetProperty("meta").GetProperty("total").GetInt64());
    }

    [Theory]
    [InlineData("sort=bogus", "sort")]
    [InlineData("pageSize=101", "pageSize")]
    [InlineData("page=0", "page")]
    [InlineData("page=abc", "page")]
    [InlineData("categoryId=abc", "categoryId")]
    public async Task List_BadQuery_Returns422WithFieldName(string query, string field)
    {
        var response = await factory.CreateClient().GetAsync($"{Path}?{query}");

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        var body = await ReadEnvelopeAsync(response);
        Assert.Equal("VALIDATION_ERROR", body.GetProperty("code").GetString());
        Assert.Equal(field, body.GetProperty("errors")[0].GetProperty("field").GetString());
    }

    [Fact]
    public async Task Get_Existing_ReturnsProductWithCategory()
    {
        var category = await SeedCategoryAsync(Unique("Drinks"));
        var product = await SeedProductAsync(Unique("tea"), categoryId: category.Id, stock: 4);

        var response = await factory.CreateClient().GetAsync($"{Path}/{product.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var data = (await ReadEnvelopeAsync(response)).GetProperty("data");
        Assert.Equal(product.Name, data.GetProperty("name").GetString());
        Assert.Equal(category.Name, data.GetProperty("categoryName").GetString());
        Assert.Equal(JsonValueKind.Null, data.GetProperty("stock").ValueKind);
    }

    [Fact]
    public async Task Get_AsAdmin_ShowsStock()
    {
        var product = await SeedProductAsync(Unique("tea"), stock: 4);

        var response = await ClientWithToken(factory.CreateToken("admin")).GetAsync($"{Path}/{product.Id}");

        var data = (await ReadEnvelopeAsync(response)).GetProperty("data");
        Assert.Equal(4, data.GetProperty("stock").GetInt32());
    }

    [Fact]
    public async Task Get_AfterCategoryDeleted_HasNullCategoryName()
    {
        var category = await SeedCategoryAsync(Unique("Drinks"));
        var product = await SeedProductAsync(Unique("tea"), categoryId: category.Id);
        await SoftDeleteCategoryAsync(category.Id);

        var response = await factory.CreateClient().GetAsync($"{Path}/{product.Id}");

        var data = (await ReadEnvelopeAsync(response)).GetProperty("data");
        Assert.Equal(JsonValueKind.Null, data.GetProperty("categoryName").ValueKind);
    }

    [Fact]
    public async Task Get_UnknownOrSoftDeleted_Returns404()
    {
        var deleted = await SeedProductAsync(Unique("gone"), deleted: true);
        var client = factory.CreateClient();

        var unknown = await client.GetAsync($"{Path}/{Guid.NewGuid()}");
        var softDeleted = await client.GetAsync($"{Path}/{deleted.Id}");

        Assert.Equal(HttpStatusCode.NotFound, unknown.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, softDeleted.StatusCode);
        Assert.Equal("NOT_FOUND", (await ReadEnvelopeAsync(softDeleted)).GetProperty("code").GetString());
    }
}
