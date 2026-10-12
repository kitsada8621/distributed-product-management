using Catalog.Domain.Products;

namespace Catalog.UnitTests.Domain;

public class ProductTests
{
    private static readonly DateTime Now = new(2026, 10, 10, 8, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Create_SetsFieldsAndTimestamps()
    {
        var categoryId = Guid.NewGuid();

        var product = Product.Create("Tea", "Green tea", 12050, categoryId, 7, Now);

        Assert.NotEqual(Guid.Empty, product.Id);
        Assert.Equal("Tea", product.Name);
        Assert.Equal("Green tea", product.Description);
        Assert.Equal(12050, product.PriceMinor);
        Assert.Equal(categoryId, product.CategoryId);
        Assert.Equal(7, product.Stock);
        Assert.False(product.IsDeleted);
        Assert.Equal(Now, product.CreatedAt);
        Assert.Equal(Now, product.UpdatedAt);
    }

    [Fact]
    public void Update_ReplacesFieldsAndKeepsStock()
    {
        var product = Product.Create("Tea", "Green tea", 12050, null, 7, Now);
        var newCategoryId = Guid.NewGuid();
        var later = Now.AddMinutes(5);

        product.Update("Coffee", "", 9900, newCategoryId, later);

        Assert.Equal("Coffee", product.Name);
        Assert.Equal("", product.Description);
        Assert.Equal(9900, product.PriceMinor);
        Assert.Equal(newCategoryId, product.CategoryId);
        Assert.Equal(7, product.Stock);
        Assert.Equal(later, product.UpdatedAt);
    }

    [Fact]
    public void SetStock_SetsAbsoluteValue()
    {
        var product = Product.Create("Tea", "", 100, null, 7, Now);
        var later = Now.AddMinutes(5);

        product.SetStock(3, later);

        Assert.Equal(3, product.Stock);
        Assert.Equal(later, product.UpdatedAt);
    }

    [Fact]
    public void ClearCategory_NullsCategoryIdAndBumpsUpdatedAt()
    {
        var product = Product.Create("Tea", "", 100, Guid.NewGuid(), 7, Now);
        var later = Now.AddMinutes(5);

        product.ClearCategory(later);

        Assert.Null(product.CategoryId);
        Assert.Equal(later, product.UpdatedAt);
    }

    [Fact]
    public void SoftDelete_SetsDeletedFlagAndTimestamps()
    {
        var product = Product.Create("Tea", "", 100, null, 7, Now);
        var later = Now.AddMinutes(5);

        product.SoftDelete(later);

        Assert.True(product.IsDeleted);
        Assert.Equal(later, product.DeletedAt);
        Assert.Equal(later, product.UpdatedAt);
    }

    [Theory]
    [InlineData(1, true)]
    [InlineData(0, false)]
    public void IsInStock_IsTrueOnlyWhenStockIsPositive(int stock, bool expected)
    {
        var product = Product.Create("Tea", "", 100, null, stock, Now);

        Assert.Equal(expected, product.IsInStock);
    }
}
