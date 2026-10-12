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

public class CategoryEndpointTests(CatalogApiFactory factory) : IClassFixture<CatalogApiFactory>
{
    private const string Path = "/api/catalog/categories";

    private HttpClient AdminClient() => ClientWithToken(factory.CreateToken("admin"));

    private async Task<Guid> CreateAsync(HttpClient admin, string name)
    {
        var response = await admin.PostAsJsonAsync(Path, new { name });
        var body = await ReadEnvelopeAsync(response);
        return body.GetProperty("data").GetProperty("id").GetGuid();
    }

    private HttpClient ClientWithToken(string token)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    private static Task<JsonElement> ReadEnvelopeAsync(HttpResponseMessage response)
    {
        return response.Content.ReadFromJsonAsync<JsonElement>();
    }

    private static string UniqueName(string prefix) => $"{prefix}-{Guid.NewGuid():N}";

    [Fact]
    public async Task List_AsGuest_ReturnsCreatedItemsWithoutDeletedOnes()
    {
        var admin = AdminClient();
        var keptName = UniqueName("kept");
        var removedName = UniqueName("removed");
        await CreateAsync(admin, keptName);
        var removedId = await CreateAsync(admin, removedName);
        await admin.DeleteAsync($"{Path}/{removedId}");

        var response = await factory.CreateClient().GetAsync(Path);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await ReadEnvelopeAsync(response);
        var names = body.GetProperty("data").EnumerateArray().Select(c => c.GetProperty("name").GetString()).ToList();
        Assert.Contains(keptName, names);
        Assert.DoesNotContain(removedName, names);
        Assert.Equal(JsonValueKind.Null, body.GetProperty("meta").ValueKind);
    }

    [Fact]
    public async Task Create_AsAdmin_Returns201WithIdAndName()
    {
        var name = UniqueName("drinks");

        var response = await AdminClient().PostAsJsonAsync(Path, new { name });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var data = (await ReadEnvelopeAsync(response)).GetProperty("data");
        Assert.Equal(name, data.GetProperty("name").GetString());
        Assert.NotEqual(Guid.Empty, data.GetProperty("id").GetGuid());
    }

    [Fact]
    public async Task Create_AsUser_Returns403()
    {
        var client = ClientWithToken(factory.CreateToken("user"));

        var response = await client.PostAsJsonAsync(Path, new { name = UniqueName("x") });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Create_WithoutToken_Returns401()
    {
        var response = await factory.CreateClient().PostAsJsonAsync(Path, new { name = UniqueName("x") });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Create_EmptyName_Returns422WithNameFieldError()
    {
        var response = await AdminClient().PostAsJsonAsync(Path, new { name = "  " });

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        var body = await ReadEnvelopeAsync(response);
        Assert.Equal("VALIDATION_ERROR", body.GetProperty("code").GetString());
        Assert.Equal("name", body.GetProperty("errors")[0].GetProperty("field").GetString());
    }

    [Fact]
    public async Task Create_DuplicateNameInDifferentCase_Returns409()
    {
        var admin = AdminClient();
        var name = UniqueName("drinks");
        await CreateAsync(admin, name);

        var response = await admin.PostAsJsonAsync(Path, new { name = name.ToUpperInvariant() });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("CATEGORY_NAME_EXISTS", (await ReadEnvelopeAsync(response)).GetProperty("code").GetString());
    }

    [Fact]
    public async Task Update_Rename_Returns200WithNewName()
    {
        var admin = AdminClient();
        var id = await CreateAsync(admin, UniqueName("old"));
        var newName = UniqueName("new");

        var response = await admin.PutAsJsonAsync($"{Path}/{id}", new { name = newName });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var data = (await ReadEnvelopeAsync(response)).GetProperty("data");
        Assert.Equal(newName, data.GetProperty("name").GetString());
    }

    [Fact]
    public async Task Update_UnknownId_Returns404()
    {
        var response = await AdminClient().PutAsJsonAsync($"{Path}/{Guid.NewGuid()}", new { name = "x" });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("NOT_FOUND", (await ReadEnvelopeAsync(response)).GetProperty("code").GetString());
    }

    [Fact]
    public async Task Update_NameOfAnotherCategory_Returns409()
    {
        var admin = AdminClient();
        var otherName = UniqueName("other");
        await CreateAsync(admin, otherName);
        var id = await CreateAsync(admin, UniqueName("mine"));

        var response = await admin.PutAsJsonAsync($"{Path}/{id}", new { name = otherName });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Update_EmptyName_Returns422()
    {
        var admin = AdminClient();
        var id = await CreateAsync(admin, UniqueName("mine"));

        var response = await admin.PutAsJsonAsync($"{Path}/{id}", new { name = "" });

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task Delete_Returns200WithNullData_ThenSecondDeleteIs404()
    {
        var admin = AdminClient();
        var id = await CreateAsync(admin, UniqueName("gone"));

        var first = await admin.DeleteAsync($"{Path}/{id}");
        var second = await admin.DeleteAsync($"{Path}/{id}");

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(JsonValueKind.Null, (await ReadEnvelopeAsync(first)).GetProperty("data").ValueKind);
        Assert.Equal(HttpStatusCode.NotFound, second.StatusCode);
    }

    [Fact]
    public async Task Delete_ThenSameNameCanBeCreatedAgain()
    {
        var admin = AdminClient();
        var name = UniqueName("reuse");
        var id = await CreateAsync(admin, name);
        await admin.DeleteAsync($"{Path}/{id}");

        var response = await admin.PostAsJsonAsync(Path, new { name });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Delete_ClearsCategoryIdOfItsProducts()
    {
        var admin = AdminClient();
        var categoryId = await CreateAsync(admin, UniqueName("with-products"));
        var productId = await InsertProductAsync(categoryId);

        var response = await admin.DeleteAsync($"{Path}/{categoryId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        await using var context = CreateContext();
        var product = await context.Products.SingleAsync(p => p.Id == productId);
        Assert.Null(product.CategoryId);
        Assert.False(product.IsDeleted);
    }

    private async Task<Guid> InsertProductAsync(Guid categoryId)
    {
        await using var context = CreateContext();
        var product = Product.Create("Tea", "", 100, categoryId, 3, DateTime.UtcNow);
        context.Products.Add(product);
        await context.SaveChangesAsync();
        return product.Id;
    }

    private CatalogDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<CatalogDbContext>()
            .UseSqlite(factory.ConnectionString)
            .AddInterceptors(new SqlitePragmaInterceptor())
            .Options;
        return new CatalogDbContext(options);
    }
}
