using Catalog.Domain.Categories;

namespace Catalog.UnitTests.Domain;

public class CategoryTests
{
    private static readonly DateTime Now = new(2026, 10, 10, 8, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Create_SetsFieldsAndTimestamps()
    {
        var category = Category.Create("Drinks", Now);

        Assert.NotEqual(Guid.Empty, category.Id);
        Assert.Equal("Drinks", category.Name);
        Assert.False(category.IsDeleted);
        Assert.Null(category.DeletedAt);
        Assert.Equal(Now, category.CreatedAt);
        Assert.Equal(Now, category.UpdatedAt);
    }

    [Fact]
    public void Rename_ChangesNameAndUpdatedAt()
    {
        var category = Category.Create("Drinks", Now);
        var later = Now.AddMinutes(5);

        category.Rename("Snacks", later);

        Assert.Equal("Snacks", category.Name);
        Assert.Equal(later, category.UpdatedAt);
        Assert.Equal(Now, category.CreatedAt);
    }

    [Fact]
    public void SoftDelete_SetsDeletedFlagAndTimestamps()
    {
        var category = Category.Create("Drinks", Now);
        var later = Now.AddMinutes(5);

        category.SoftDelete(later);

        Assert.True(category.IsDeleted);
        Assert.Equal(later, category.DeletedAt);
        Assert.Equal(later, category.UpdatedAt);
    }
}
