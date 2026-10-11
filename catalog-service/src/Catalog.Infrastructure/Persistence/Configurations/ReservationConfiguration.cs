using Catalog.Domain.Reservations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Catalog.Infrastructure.Persistence.Configurations;

public sealed class ReservationConfiguration : IEntityTypeConfiguration<Reservation>
{
    public void Configure(EntityTypeBuilder<Reservation> builder)
    {
        builder.ToTable("reservations", table =>
            table.HasCheckConstraint("ck_reservations_status", "status IN ('Reserved', 'Committed', 'Released')"));

        // One order = one reservation, so the primary key also makes order.created idempotent.
        builder.HasKey(r => r.OrderId);
        builder.Property(r => r.OrderId).HasColumnName("order_id");
        builder.Property(r => r.Status).HasColumnName("status").HasConversion<string>();
        builder.Property(r => r.CreatedAt).HasColumnName("created_at");
        builder.Property(r => r.UpdatedAt).HasColumnName("updated_at");

        builder.HasMany(r => r.Items)
            .WithOne()
            .HasForeignKey(item => item.ReservationId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
