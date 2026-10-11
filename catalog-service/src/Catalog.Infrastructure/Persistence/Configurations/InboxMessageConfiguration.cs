using Catalog.Domain.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Catalog.Infrastructure.Persistence.Configurations;

public sealed class InboxMessageConfiguration : IEntityTypeConfiguration<InboxMessage>
{
    public void Configure(EntityTypeBuilder<InboxMessage> builder)
    {
        builder.ToTable("inbox");

        builder.HasKey(m => m.EventId);
        builder.Property(m => m.EventId).HasColumnName("event_id");
        builder.Property(m => m.Topic).HasColumnName("topic").IsRequired();
        builder.Property(m => m.ProcessedAt).HasColumnName("processed_at");
    }
}
