using Auth.Domain.Users;
using Auth.Infrastructure.Persistence;
using Auth.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace Auth.IntegrationTests.Persistence;

public sealed class AdminSeederTests : IDisposable
{
    private readonly TestDatabase _database = new();
    private readonly Argon2PasswordHasher _hasher = new();

    public void Dispose() => _database.Dispose();

    [Fact]
    public async Task Seed_CreatesOneAdminWhoseHashVerifiesTheConfiguredPassword()
    {
        await SeedAsync(new SeedOptions { AdminEmail = "admin@example.com", AdminPassword = "Admin#12345" });

        await using var context = _database.CreateContext();
        var admin = await context.Users.SingleAsync();
        Assert.Equal(UserRole.Admin, admin.Role);
        Assert.Equal("Administrator", admin.Name);
        Assert.True(_hasher.Verify("Admin#12345", admin.PasswordHash));
    }

    [Fact]
    public async Task Seed_SecondRun_CreatesNothing()
    {
        var options = new SeedOptions { AdminEmail = "admin@example.com", AdminPassword = "Admin#12345" };
        await SeedAsync(options);

        await SeedAsync(options);

        await using var context = _database.CreateContext();
        Assert.Equal(1, await context.Users.CountAsync());
    }

    [Theory]
    [InlineData("", "Admin#12345")]
    [InlineData("admin@example.com", "")]
    public async Task Seed_EmptyOptions_CreatesNothing(string email, string password)
    {
        await SeedAsync(new SeedOptions { AdminEmail = email, AdminPassword = password });

        await using var context = _database.CreateContext();
        Assert.Equal(0, await context.Users.CountAsync());
    }

    [Fact]
    public async Task Seed_ExistingNormalUserWithSameEmail_IsLeftUntouched()
    {
        await using (var context = _database.CreateContext())
        {
            context.Users.Add(User.Create("admin@example.com", "Normal", "hash", UserRole.User, DateTime.UtcNow));
            await context.SaveChangesAsync();
        }

        await SeedAsync(new SeedOptions { AdminEmail = "ADMIN@example.com", AdminPassword = "Admin#12345" });

        await using var reader = _database.CreateContext();
        var user = await reader.Users.SingleAsync();
        Assert.Equal(UserRole.User, user.Role);
        Assert.Equal("hash", user.PasswordHash);
    }

    private async Task SeedAsync(SeedOptions options)
    {
        await using var context = _database.CreateContext();
        var seeder = new AdminSeeder(context, _hasher, options, TimeProvider.System, NullLogger<AdminSeeder>.Instance);
        await seeder.SeedAsync(CancellationToken.None);
    }
}
