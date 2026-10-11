using Catalog.Domain.Reservations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Catalog.Infrastructure.Persistence.Configurations;

public sealed class ReservationItemConfiguration : IEntityTypeConfiguration<ReservationItem>
{
    public void Configure(EntityTypeBuilder<ReservationItem> builder)
    {
        builder.ToTable("reservation_items", table =>
            table.HasCheckConstraint("ck_reservation_items_quantity", "quantity > 0"));

        builder.HasKey(i => new { i.ReservationId, i.ProductId });
        builder.Property(i => i.ReservationId).HasColumnName("reservation_id");
        // No FK on product_id: stock can be returned even when the product was soft deleted.
        builder.Property(i => i.ProductId).HasColumnName("product_id");
        builder.Property(i => i.Quantity).HasColumnName("quantity");
    }
}
