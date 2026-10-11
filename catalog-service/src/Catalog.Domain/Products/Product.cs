namespace Catalog.Domain.Products;

public class Product
{
    public Guid Id { get; private set; }
    public Guid? CategoryId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public long PriceMinor { get; private set; }
    public int Stock { get; private set; }
    public bool IsDeleted { get; private set; }
    public DateTime? DeletedAt { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    public bool IsInStock => Stock > 0;

    private Product()
    {
    }

    public static Product Create(
        string name,
        string description,
        long priceMinor,
        Guid? categoryId,
        int stock,
        DateTime now)
    {
        return new Product
        {
            Id = Guid.NewGuid(),
            CategoryId = categoryId,
            Name = name,
            Description = description,
            PriceMinor = priceMinor,
            Stock = stock,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void Update(string name, string description, long priceMinor, Guid? categoryId, DateTime now)
    {
        Name = name;
        Description = description;
        PriceMinor = priceMinor;
        CategoryId = categoryId;
        UpdatedAt = now;
    }

    public void SetStock(int quantity, DateTime now)
    {
        Stock = quantity;
        UpdatedAt = now;
    }

    public void ClearCategory(DateTime now)
    {
        CategoryId = null;
        UpdatedAt = now;
    }

    public void SoftDelete(DateTime now)
    {
        IsDeleted = true;
        DeletedAt = now;
        UpdatedAt = now;
    }
}
