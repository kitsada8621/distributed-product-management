using Catalog.Domain.Categories;
using Catalog.Domain.Products;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Catalog.Infrastructure.Persistence.Configurations;

public sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("products", table =>
        {
            table.HasCheckConstraint("ck_products_price_minor", "price_minor > 0");
            table.HasCheckConstraint("ck_products_stock", "stock >= 0");
            table.HasCheckConstraint("ck_products_is_deleted", "is_deleted IN (0, 1)");
        });

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).HasColumnName("id");
        builder.Property(p => p.CategoryId).HasColumnName("category_id");
        builder.Property(p => p.Name).HasColumnName("name").IsRequired();
        builder.Property(p => p.Description)
            .HasColumnName("description")
            .IsRequired()
            .HasDefaultValue(string.Empty)
            .ValueGeneratedNever();
        builder.Property(p => p.PriceMinor).HasColumnName("price_minor");
        builder.Property(p => p.Stock)
            .HasColumnName("stock")
            .HasDefaultValue(0)
            .ValueGeneratedNever();
        builder.Property(p => p.IsDeleted)
            .HasColumnName("is_deleted")
            .HasDefaultValue(false)
            .ValueGeneratedNever();
        builder.Property(p => p.DeletedAt).HasColumnName("deleted_at");
        builder.Property(p => p.CreatedAt).HasColumnName("created_at");
        builder.Property(p => p.UpdatedAt).HasColumnName("updated_at");

        builder.HasOne<Category>()
            .WithMany()
            .HasForeignKey(p => p.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(p => new { p.CategoryId, p.Name })
            .HasDatabaseName("ix_products_list")
            .HasFilter("is_deleted = 0");
        builder.HasIndex(p => p.Name)
            .HasDatabaseName("ix_products_name")
            .HasFilter("is_deleted = 0");
    }
}
