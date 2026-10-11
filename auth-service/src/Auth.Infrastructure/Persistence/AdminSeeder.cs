using Auth.Application.Security;
using Auth.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Auth.Infrastructure.Persistence;

public sealed class AdminSeeder(
    AuthDbContext context,
    IPasswordHasher passwordHasher,
    SeedOptions options,
    TimeProvider timeProvider,
    ILogger<AdminSeeder> logger)
{
    public async Task SeedAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(options.AdminEmail) || string.IsNullOrEmpty(options.AdminPassword))
        {
            logger.LogWarning("Seed:AdminEmail or Seed:AdminPassword is empty; no admin account was created");
            return;
        }

        var exists = await context.Users.AnyAsync(u => u.Email == options.AdminEmail, cancellationToken);
        if (exists)
        {
            return;
        }

        var admin = User.Create(
            options.AdminEmail.Trim(),
            options.AdminName,
            passwordHasher.Hash(options.AdminPassword),
            UserRole.Admin,
            timeProvider.GetUtcNow().UtcDateTime);
        context.Users.Add(admin);
        await context.SaveChangesAsync(cancellationToken);
    }
}
