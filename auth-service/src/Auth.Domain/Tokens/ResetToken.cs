namespace Auth.Domain.Tokens;

public class ResetToken
{
    private static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(30);

    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public string TokenHash { get; private set; } = string.Empty;
    public DateTime ExpiresAt { get; private set; }
    public DateTime? UsedAt { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private ResetToken()
    {
    }

    public static ResetToken Issue(Guid userId, string tokenHash, DateTime now)
    {
        return new ResetToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TokenHash = tokenHash,
            ExpiresAt = now + Lifetime,
            CreatedAt = now
        };
    }

    public bool IsUsable(DateTime now)
    {
        return UsedAt is null && ExpiresAt > now;
    }

    public void MarkUsed(DateTime now)
    {
        UsedAt = now;
    }
}
