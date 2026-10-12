using Auth.Domain.Tokens;

namespace Auth.UnitTests.Domain;

public class TokenTests
{
    private static readonly DateTime Now = new(2026, 10, 10, 8, 30, 0, DateTimeKind.Utc);

    private static RefreshToken NewRefreshToken() => RefreshToken.Issue(Guid.NewGuid(), Guid.NewGuid(), "hash", Now);

    [Fact]
    public void RefreshToken_ExpiresAfterSevenDays()
    {
        var token = NewRefreshToken();

        Assert.False(token.IsExpired(Now.AddDays(7).AddSeconds(-1)));
        Assert.True(token.IsExpired(Now.AddDays(7)));
    }

    [Fact]
    public void RefreshToken_MarkUsed_SetsUsed()
    {
        var token = NewRefreshToken();

        token.MarkUsed(Now);

        Assert.True(token.IsUsed);
        Assert.False(token.IsRevoked);
    }

    [Fact]
    public void RefreshToken_Revoke_KeepsFirstTimestamp()
    {
        var token = NewRefreshToken();

        token.Revoke(Now);
        token.Revoke(Now.AddHours(1));

        Assert.True(token.IsRevoked);
        Assert.Equal(Now, token.RevokedAt);
    }

    [Fact]
    public void ResetToken_IsUsableWithinThirtyMinutes()
    {
        var token = ResetToken.Issue(Guid.NewGuid(), "hash", Now);

        Assert.True(token.IsUsable(Now.AddMinutes(29)));
        Assert.False(token.IsUsable(Now.AddMinutes(30)));
    }

    [Fact]
    public void ResetToken_AfterMarkUsed_IsNotUsable()
    {
        var token = ResetToken.Issue(Guid.NewGuid(), "hash", Now);

        token.MarkUsed(Now);

        Assert.False(token.IsUsable(Now));
    }
}
