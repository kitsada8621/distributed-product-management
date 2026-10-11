using Auth.Domain.Emails;
using Auth.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Auth.Infrastructure.Persistence.Configurations;

public sealed class EmailOutboxConfiguration : IEntityTypeConfiguration<EmailOutbox>
{
    public void Configure(EntityTypeBuilder<EmailOutbox> builder)
    {
        builder.ToTable("email_outbox", table =>
            table.HasCheckConstraint("ck_email_outbox_status", "status IN ('Pending', 'Sent', 'Failed')"));

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id");
        builder.Property(e => e.UserId).HasColumnName("user_id");
        builder.Property(e => e.ToEmail).HasColumnName("to_email").IsRequired();
        builder.Property(e => e.Subject).HasColumnName("subject").IsRequired();
        builder.Property(e => e.Body).HasColumnName("body").IsRequired();
        builder.Property(e => e.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasDefaultValueSql("'Pending'")
            .ValueGeneratedNever();
        builder.Property(e => e.Attempts)
            .HasColumnName("attempts")
            .HasDefaultValue(0)
            .ValueGeneratedNever();
        builder.Property(e => e.NextAttemptAt).HasColumnName("next_attempt_at");
        builder.Property(e => e.LastError).HasColumnName("last_error");
        builder.Property(e => e.CreatedAt).HasColumnName("created_at");
        builder.Property(e => e.SentAt).HasColumnName("sent_at");

        builder.HasOne<User>().WithMany().HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(e => e.NextAttemptAt)
            .HasDatabaseName("ix_email_outbox_pending")
            .HasFilter("status = 'Pending'");
    }
}
