using Auth.Domain.Users;
using Auth.Infrastructure.Persistence;
using Auth.IntegrationTests.Support;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Auth.IntegrationTests.Endpoints;

public class StartupTests(AuthApiFactory factory) : IClassFixture<AuthApiFactory>
{
    [Fact]
    public async Task Startup_MigratesDatabaseAndSeedsTheAdmin()
    {
        using var client = factory.CreateClient();

        await using var scope = factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
        Assert.NotEmpty(await context.Database.GetAppliedMigrationsAsync());
        Assert.Empty(await context.RefreshTokens.ToListAsync());
        Assert.Empty(await context.ResetTokens.ToListAsync());
        Assert.Empty(await context.EmailOutboxes.ToListAsync());
        var admin = await context.Users.SingleAsync(u => u.Email == AuthApiFactory.AdminEmail);
        Assert.Equal(UserRole.Admin, admin.Role);
    }

    [Fact]
    public async Task Startup_SecondHostOnTheSameDatabase_DoesNotDuplicateTheAdmin()
    {
        using var first = factory.CreateClient();

        await using var secondFactory = factory.WithWebHostBuilder(_ => { });
        using var second = secondFactory.CreateClient();

        await using var scope = factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
        Assert.Equal(1, await context.Users.CountAsync(u => u.Email == AuthApiFactory.AdminEmail));
    }
}
