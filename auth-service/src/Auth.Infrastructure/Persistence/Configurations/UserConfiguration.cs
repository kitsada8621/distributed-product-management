using Auth.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Auth.Infrastructure.Persistence.Configurations;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users", table =>
        {
            table.HasCheckConstraint("ck_users_role", "role IN ('user', 'admin')");
            table.HasCheckConstraint("ck_users_failed_attempts", "failed_attempts >= 0");
        });

        builder.HasKey(u => u.Id);
        builder.Property(u => u.Id).HasColumnName("id");
        builder.Property(u => u.Email).HasColumnName("email").IsRequired().UseCollation("NOCASE");
        builder.Property(u => u.Name).HasColumnName("name").IsRequired();
        builder.Property(u => u.PasswordHash).HasColumnName("password_hash").IsRequired();
        builder.Property(u => u.Role)
            .HasColumnName("role")
            .HasConversion(role => role.ToString().ToLowerInvariant(), text => Enum.Parse<UserRole>(text, true))
            .HasDefaultValueSql("'user'")
            .ValueGeneratedNever();
        builder.Property(u => u.FailedAttempts)
            .HasColumnName("failed_attempts")
            .HasDefaultValue(0)
            .ValueGeneratedNever();
        builder.Property(u => u.LockedUntil).HasColumnName("locked_until");
        builder.Property(u => u.CreatedAt).HasColumnName("created_at");
        builder.Property(u => u.UpdatedAt).HasColumnName("updated_at");

        builder.HasIndex(u => u.Email).IsUnique().HasDatabaseName("ux_users_email");
    }
}
