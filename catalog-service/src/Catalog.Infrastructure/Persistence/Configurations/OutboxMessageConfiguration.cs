using Catalog.Domain.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Catalog.Infrastructure.Persistence.Configurations;

public sealed class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("outbox", table =>
            table.HasCheckConstraint("ck_outbox_status", "status IN ('Pending', 'Sent')"));

        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id).HasColumnName("id");
        builder.Property(m => m.Topic).HasColumnName("topic").IsRequired();
        builder.Property(m => m.MessageKey).HasColumnName("message_key").IsRequired();
        builder.Property(m => m.Payload).HasColumnName("payload").IsRequired();
        builder.Property(m => m.CorrelationId).HasColumnName("correlation_id").IsRequired();
        builder.Property(m => m.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasDefaultValueSql("'Pending'")
            .ValueGeneratedNever();
        builder.Property(m => m.Attempts)
            .HasColumnName("attempts")
            .HasDefaultValue(0)
            .ValueGeneratedNever();
        builder.Property(m => m.NextAttemptAt).HasColumnName("next_attempt_at");
        builder.Property(m => m.CreatedAt).HasColumnName("created_at");
        builder.Property(m => m.SentAt).HasColumnName("sent_at");

        // The relay polls Pending rows that are due.
        builder.HasIndex(m => m.NextAttemptAt)
            .HasDatabaseName("ix_outbox_pending")
            .HasFilter("status = 'Pending'");
    }
}
