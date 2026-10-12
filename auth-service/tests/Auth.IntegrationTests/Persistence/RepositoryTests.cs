using Auth.Domain.Emails;
using Auth.Domain.Tokens;
using Auth.Domain.Users;
using Auth.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Auth.IntegrationTests.Persistence;

public sealed class RepositoryTests : IDisposable
{
    private static readonly DateTime Now = new(2026, 10, 10, 8, 30, 0, DateTimeKind.Utc);

    private readonly TestDatabase _database = new();

    public void Dispose() => _database.Dispose();

    [Fact]
    public async Task Users_AddThenRead_RoundTripsAndFindsByEmailIgnoringCase()
    {
        var user = await SeedUserAsync("user@example.com");

        await using var context = _database.CreateContext();
        var repository = new UserRepository(context);

        var byEmail = await repository.GetByEmailAsync("USER@Example.com", CancellationToken.None);
        var byId = await repository.GetByIdAsync(user.Id, CancellationToken.None);
        var missing = await repository.GetByEmailAsync("nobody@example.com", CancellationToken.None);

        Assert.Equal(user.Id, byEmail?.Id);
        Assert.Equal("Somchai", byId?.Name);
        Assert.Null(missing);
    }

    [Fact]
    public async Task RefreshTokens_FindByHashFamilyAndUser()
    {
        var user = await SeedUserAsync("user@example.com");
        var familyId = Guid.NewGuid();
        await using (var context = _database.CreateContext())
        {
            var repository = new RefreshTokenRepository(context);
            await repository.AddAsync(RefreshToken.Issue(user.Id, familyId, "hash-1", Now), CancellationToken.None);
            await repository.AddAsync(RefreshToken.Issue(user.Id, familyId, "hash-2", Now), CancellationToken.None);
            await repository.AddAsync(RefreshToken.Issue(user.Id, Guid.NewGuid(), "hash-3", Now), CancellationToken.None);
            await context.SaveChangesAsync();
        }

        await using var reader = _database.CreateContext();
        var read = new RefreshTokenRepository(reader);

        Assert.Equal(familyId, (await read.GetByHashAsync("hash-2", CancellationToken.None))?.FamilyId);
        Assert.Null(await read.GetByHashAsync("unknown", CancellationToken.None));
        Assert.Equal(2, (await read.GetFamilyAsync(familyId, CancellationToken.None)).Count);
        Assert.Equal(3, (await read.GetByUserAsync(user.Id, CancellationToken.None)).Count);
    }

    [Fact]
    public async Task RefreshTokens_RevokeOnTrackedFamily_PersistsAfterSave()
    {
        var user = await SeedUserAsync("user@example.com");
        var familyId = Guid.NewGuid();
        await using (var context = _database.CreateContext())
        {
            await new RefreshTokenRepository(context).AddAsync(
                RefreshToken.Issue(user.Id, familyId, "hash-1", Now), CancellationToken.None);
            await context.SaveChangesAsync();
        }

        await using (var context = _database.CreateContext())
        {
            var family = await new RefreshTokenRepository(context).GetFamilyAsync(familyId, CancellationToken.None);
            foreach (var token in family)
            {
                token.Revoke(Now);
            }
            await context.SaveChangesAsync();
        }

        await using var reader = _database.CreateContext();
        var reloaded = await reader.RefreshTokens.SingleAsync();
        Assert.True(reloaded.IsRevoked);
        Assert.Equal(Now, reloaded.RevokedAt);
    }

    [Fact]
    public async Task RefreshTokens_DuplicateHash_FailsOnSave()
    {
        var user = await SeedUserAsync("user@example.com");
        await using var context = _database.CreateContext();
        var repository = new RefreshTokenRepository(context);
        await repository.AddAsync(RefreshToken.Issue(user.Id, Guid.NewGuid(), "same-hash", Now), CancellationToken.None);
        await repository.AddAsync(RefreshToken.Issue(user.Id, Guid.NewGuid(), "same-hash", Now), CancellationToken.None);

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    [Fact]
    public async Task ResetTokens_AddThenFindByHash()
    {
        var user = await SeedUserAsync("user@example.com");
        await using (var context = _database.CreateContext())
        {
            await new ResetTokenRepository(context).AddAsync(
                ResetToken.Issue(user.Id, "reset-hash", Now), CancellationToken.None);
            await context.SaveChangesAsync();
        }

        await using var reader = _database.CreateContext();
        var repository = new ResetTokenRepository(reader);

        var found = await repository.GetByHashAsync("reset-hash", CancellationToken.None);
        Assert.Equal(user.Id, found?.UserId);
        Assert.Equal(Now.AddMinutes(30), found?.ExpiresAt);
        Assert.Null(await repository.GetByHashAsync("unknown", CancellationToken.None));
    }

    [Fact]
    public async Task EmailOutbox_GetDue_ReturnsOnlyDuePendingRowsOldestFirst()
    {
        var user = await SeedUserAsync("user@example.com");
        var newerDue = NewEmail(user, "newer@example.com", Now.AddMinutes(-1));
        var olderDue = NewEmail(user, "older@example.com", Now.AddMinutes(-2));
        var future = NewEmail(user, "future@example.com", Now.AddMinutes(1));
        var sent = NewEmail(user, "sent@example.com", Now.AddMinutes(-3));
        sent.MarkSent(Now);
        var failed = NewEmail(user, "failed@example.com", Now.AddMinutes(-4));
        for (var i = 0; i < 5; i++)
        {
            failed.MarkAttemptFailed("smtp down", Now.AddMinutes(-5));
        }
        await using (var context = _database.CreateContext())
        {
            var repository = new EmailOutboxRepository(context);
            foreach (var email in new[] { newerDue, olderDue, future, sent, failed })
            {
                await repository.AddAsync(email, CancellationToken.None);
            }
            await context.SaveChangesAsync();
        }

        await using var reader = _database.CreateContext();
        var due = await new EmailOutboxRepository(reader).GetDueAsync(Now, 20, CancellationToken.None);

        Assert.Equal(["older@example.com", "newer@example.com"], due.Select(e => e.ToEmail));
    }

    [Fact]
    public async Task EmailOutbox_GetDue_RespectsBatchSize()
    {
        var user = await SeedUserAsync("user@example.com");
        await using (var context = _database.CreateContext())
        {
            var repository = new EmailOutboxRepository(context);
            for (var i = 0; i < 3; i++)
            {
                await repository.AddAsync(NewEmail(user, $"u{i}@example.com", Now.AddMinutes(-i - 1)), CancellationToken.None);
            }
            await context.SaveChangesAsync();
        }

        await using var reader = _database.CreateContext();
        var due = await new EmailOutboxRepository(reader).GetDueAsync(Now, 2, CancellationToken.None);

        Assert.Equal(2, due.Count);
    }

    private async Task<User> SeedUserAsync(string email)
    {
        var user = User.Create(email, "Somchai", "hash", UserRole.User, Now);
        await using var context = _database.CreateContext();
        await new UserRepository(context).AddAsync(user, CancellationToken.None);
        await context.SaveChangesAsync();
        return user;
    }

    private static EmailOutbox NewEmail(User user, string to, DateTime dueAt)
    {
        return EmailOutbox.Create(user.Id, to, "Subject", "Body", dueAt);
    }
}
