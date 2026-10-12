using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Catalog.Domain.Categories;
using Catalog.Domain.Products;
using Catalog.Infrastructure.Persistence;
using Catalog.IntegrationTests.Support;
using Microsoft.EntityFrameworkCore;

namespace Catalog.IntegrationTests.Endpoints;

public class ProductWriteEndpointTests(CatalogApiFactory factory) : IClassFixture<CatalogApiFactory>
{
    private const string Path = "/api/catalog/products";

    private static readonly DateTime Now = new(2026, 10, 10, 8, 30, 0, DateTimeKind.Utc);

    private HttpClient AdminClient() => ClientWithToken(factory.CreateToken("admin"));

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

    private static StringContent Json(string json)
    {
        return new StringContent(json, Encoding.UTF8, "application/json");
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

    private async Task<Category> SeedCategoryAsync(bool deleted = false)
    {
        await using var context = CreateContext();
        var category = Category.Create($"cat-{Guid.NewGuid():N}", Now);
        if (deleted)
        {
            category.SoftDelete(Now);
        }
        context.Categories.Add(category);
        await context.SaveChangesAsync();
        return category;
    }

    private async Task<Product> SeedProductAsync(Guid? categoryId = null, int stock = 5, bool deleted = false)
    {
        await using var context = CreateContext();
        var product = Product.Create($"prod-{Guid.NewGuid():N}", "Old desc", 12900, categoryId, stock, Now);
        if (deleted)
        {
            product.SoftDelete(Now);
        }
        context.Products.Add(product);
        await context.SaveChangesAsync();
        return product;
    }

    private async Task<Product> ReloadAsync(Guid id)
    {
        await using var context = CreateContext();
        return await context.Products.AsNoTracking().SingleAsync(p => p.Id == id);
    }

    private static object ValidCreate(
        string name = "Green tea",
        decimal price = 1290.00m,
        object? categoryId = null,
        decimal stock = 10)
    {
        return new { name, description = "Hot", price, categoryId, stock };
    }

    private static async Task AssertValidationFieldAsync(HttpResponseMessage response, string field)
    {
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        var body = await ReadEnvelopeAsync(response);
        Assert.Equal("VALIDATION_ERROR", body.GetProperty("code").GetString());
        var fields = body.GetProperty("errors").EnumerateArray().Select(e => e.GetProperty("field").GetString());
        Assert.Contains(field, fields);
    }

    // ---- P3 create ----

    [Fact]
    public async Task Create_AsAdmin_Returns201WithProduct()
    {
        var response = await AdminClient().PostAsJsonAsync(Path, ValidCreate());

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var raw = await response.Content.ReadAsStringAsync();
        Assert.Contains("\"price\":1290.00", raw);
        var data = JsonDocument.Parse(raw).RootElement.GetProperty("data");
        Assert.True(data.GetProperty("inStock").GetBoolean());
        Assert.Equal(10, data.GetProperty("stock").GetInt32());
        Assert.Equal(JsonValueKind.Null, data.GetProperty("categoryId").ValueKind);
        Assert.Equal(JsonValueKind.Null, data.GetProperty("categoryName").ValueKind);
    }

    [Fact]
    public async Task Create_WithCategory_ReturnsCategoryName()
    {
        var category = await SeedCategoryAsync();

        var response = await AdminClient().PostAsJsonAsync(Path, ValidCreate(categoryId: category.Id));

        var data = (await ReadEnvelopeAsync(response)).GetProperty("data");
        Assert.Equal(category.Name, data.GetProperty("categoryName").GetString());
    }

    [Fact]
    public async Task Create_AsUser_Returns403()
    {
        var response = await ClientWithToken(factory.CreateToken("user")).PostAsJsonAsync(Path, ValidCreate());

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Create_WithoutToken_Returns401()
    {
        var response = await ClientWithToken(null).PostAsJsonAsync(Path, ValidCreate());

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-5")]
    [InlineData("12.345")]
    public async Task Create_BadPrice_Returns422OnPrice(string price)
    {
        var body = $"{{\"name\":\"Tea\",\"price\":{price},\"stock\":1}}";

        var response = await AdminClient().PostAsync(Path, Json(body));

        await AssertValidationFieldAsync(response, "price");
    }

    [Fact]
    public async Task Create_EmptyName_Returns422OnName()
    {
        var response = await AdminClient().PostAsJsonAsync(Path, ValidCreate(name: ""));

        await AssertValidationFieldAsync(response, "name");
    }

    [Theory]
    [InlineData("-1")]
    [InlineData("1.5")]
    public async Task Create_BadStock_Returns422OnStock(string stock)
    {
        var body = $"{{\"name\":\"Tea\",\"price\":10,\"stock\":{stock}}}";

        var response = await AdminClient().PostAsync(Path, Json(body));

        await AssertValidationFieldAsync(response, "stock");
    }

    [Fact]
    public async Task Create_UnknownCategory_Returns422OnCategoryId()
    {
        var response = await AdminClient().PostAsJsonAsync(Path, ValidCreate(categoryId: Guid.NewGuid()));

        await AssertValidationFieldAsync(response, "categoryId");
    }

    [Fact]
    public async Task Create_DeletedCategory_Returns422OnCategoryId()
    {
        var category = await SeedCategoryAsync(deleted: true);

        var response = await AdminClient().PostAsJsonAsync(Path, ValidCreate(categoryId: category.Id));

        await AssertValidationFieldAsync(response, "categoryId");
    }

    // ---- P4 update ----

    [Fact]
    public async Task Update_ReplacesFieldsAndLeavesStockUnchanged()
    {
        var product = await SeedProductAsync(stock: 8);
        var category = await SeedCategoryAsync();

        var body = new { name = "Renamed", description = "New desc", price = 55.50m, categoryId = category.Id };
        var response = await AdminClient().PutAsJsonAsync($"{Path}/{product.Id}", body);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var saved = await ReloadAsync(product.Id);
        Assert.Equal("Renamed", saved.Name);
        Assert.Equal("New desc", saved.Description);
        Assert.Equal(5550, saved.PriceMinor);
        Assert.Equal(category.Id, saved.CategoryId);
        Assert.Equal(8, saved.Stock);
    }

    [Fact]
    public async Task Update_OmittedDescription_BecomesEmptyString()
    {
        var product = await SeedProductAsync();

        var response = await AdminClient().PutAsync($"{Path}/{product.Id}", Json("{\"name\":\"Tea\",\"price\":10}"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(string.Empty, (await ReloadAsync(product.Id)).Description);
    }

    [Fact]
    public async Task Update_NullCategoryId_ClearsTheCategory()
    {
        var category = await SeedCategoryAsync();
        var product = await SeedProductAsync(categoryId: category.Id);

        var response = await AdminClient().PutAsync(
            $"{Path}/{product.Id}", Json("{\"name\":\"Tea\",\"price\":10,\"categoryId\":null}"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Null((await ReloadAsync(product.Id)).CategoryId);
    }

    [Fact]
    public async Task Update_UnknownOrDeletedProduct_Returns404()
    {
        var deleted = await SeedProductAsync(deleted: true);
        var body = new { name = "Tea", price = 10 };
        var admin = AdminClient();

        var unknown = await admin.PutAsJsonAsync($"{Path}/{Guid.NewGuid()}", body);
        var softDeleted = await admin.PutAsJsonAsync($"{Path}/{deleted.Id}", body);

        Assert.Equal(HttpStatusCode.NotFound, unknown.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, softDeleted.StatusCode);
    }

    [Fact]
    public async Task Update_BadPrice_Returns422OnPrice()
    {
        var product = await SeedProductAsync();

        var response = await AdminClient().PutAsJsonAsync($"{Path}/{product.Id}", new { name = "Tea", price = 0 });

        await AssertValidationFieldAsync(response, "price");
    }

    // ---- P5 delete ----

    [Fact]
    public async Task Delete_SoftDeletes_ThenReadsAndListShowNothing()
    {
        var product = await SeedProductAsync();
        var admin = AdminClient();

        var first = await admin.DeleteAsync($"{Path}/{product.Id}");
        var second = await admin.DeleteAsync($"{Path}/{product.Id}");
        var get = await factory.CreateClient().GetAsync($"{Path}/{product.Id}");
        var list = await factory.CreateClient().GetAsync($"{Path}?q={product.Name}");

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(JsonValueKind.Null, (await ReadEnvelopeAsync(first)).GetProperty("data").ValueKind);
        Assert.Equal(HttpStatusCode.NotFound, second.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, get.StatusCode);
        Assert.Empty((await ReadEnvelopeAsync(list)).GetProperty("data").EnumerateArray());
        Assert.True((await ReloadAsync(product.Id)).IsDeleted);
    }

    [Fact]
    public async Task Delete_AsUser_Returns403()
    {
        var product = await SeedProductAsync();

        var response = await ClientWithToken(factory.CreateToken("user")).DeleteAsync($"{Path}/{product.Id}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // ---- P6 stock ----

    [Theory]
    [InlineData(40)]
    [InlineData(0)]
    public async Task SetStock_SetsAbsoluteValue(int quantity)
    {
        var product = await SeedProductAsync(stock: 5);

        var response = await AdminClient().PutAsJsonAsync($"{Path}/{product.Id}/stock", new { quantity });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var data = (await ReadEnvelopeAsync(response)).GetProperty("data");
        Assert.Equal(quantity, data.GetProperty("stock").GetInt32());
        Assert.Equal(quantity, (await ReloadAsync(product.Id)).Stock);
    }

    [Fact]
    public async Task SetStock_Negative_Returns422StockNegative()
    {
        var product = await SeedProductAsync(stock: 5);

        var response = await AdminClient().PutAsJsonAsync($"{Path}/{product.Id}/stock", new { quantity = -1 });

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.Equal("STOCK_NEGATIVE", (await ReadEnvelopeAsync(response)).GetProperty("code").GetString());
        Assert.Equal(5, (await ReloadAsync(product.Id)).Stock);
    }

    [Theory]
    [InlineData("{\"quantity\":4.5}")]
    [InlineData("{}")]
    public async Task SetStock_FractionalOrMissing_Returns422ValidationError(string body)
    {
        var product = await SeedProductAsync();

        var response = await AdminClient().PutAsync($"{Path}/{product.Id}/stock", Json(body));

        await AssertValidationFieldAsync(response, "quantity");
    }

    [Fact]
    public async Task SetStock_UnknownProduct_Returns404()
    {
        var response = await AdminClient().PutAsJsonAsync($"{Path}/{Guid.NewGuid()}/stock", new { quantity = 3 });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task SetStock_AsUser_Returns403()
    {
        var product = await SeedProductAsync();

        var response = await ClientWithToken(factory.CreateToken("user"))
            .PutAsJsonAsync($"{Path}/{product.Id}/stock", new { quantity = 3 });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
