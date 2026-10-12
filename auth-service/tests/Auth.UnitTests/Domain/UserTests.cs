using Auth.Domain.Users;

namespace Auth.UnitTests.Domain;

public class UserTests
{
    private static readonly DateTime Now = new(2026, 10, 10, 8, 30, 0, DateTimeKind.Utc);

    private static User NewUser() => User.Create("user@example.com", "Somchai", "hash", UserRole.User, Now);

    [Fact]
    public void Create_SetsInitialValues()
    {
        var user = NewUser();

        Assert.NotEqual(Guid.Empty, user.Id);
        Assert.Equal(UserRole.User, user.Role);
        Assert.Equal(0, user.FailedAttempts);
        Assert.Null(user.LockedUntil);
    }

    [Fact]
    public void RecordFailedLogin_FourFailures_DoesNotLock()
    {
        var user = NewUser();

        for (var i = 0; i < 4; i++)
        {
            user.RecordFailedLogin(Now);
        }

        Assert.False(user.IsLocked(Now));
        Assert.Equal(4, user.FailedAttempts);
    }

    [Fact]
    public void RecordFailedLogin_FifthFailure_LocksForFifteenMinutesAndResetsCounter()
    {
        var user = NewUser();

        for (var i = 0; i < 5; i++)
        {
            user.RecordFailedLogin(Now);
        }

        Assert.True(user.IsLocked(Now));
        Assert.Equal(Now.AddMinutes(15), user.LockedUntil);
        Assert.Equal(0, user.FailedAttempts);
    }

    [Fact]
    public void IsLocked_AfterLockedUntil_ReturnsFalse()
    {
        var user = NewUser();
        for (var i = 0; i < 5; i++)
        {
            user.RecordFailedLogin(Now);
        }

        Assert.True(user.IsLocked(Now.AddMinutes(14)));
        Assert.False(user.IsLocked(Now.AddMinutes(15)));
    }

    [Fact]
    public void RecordSuccessfulLogin_ClearsCounterAndLock()
    {
        var user = NewUser();
        user.RecordFailedLogin(Now);
        user.RecordFailedLogin(Now);

        user.RecordSuccessfulLogin(Now);

        Assert.Equal(0, user.FailedAttempts);
        Assert.Null(user.LockedUntil);
    }

    [Fact]
    public void ChangePassword_ReplacesHash()
    {
        var user = NewUser();

        user.ChangePassword("new-hash", Now.AddHours(1));

        Assert.Equal("new-hash", user.PasswordHash);
        Assert.Equal(Now.AddHours(1), user.UpdatedAt);
    }
}
