namespace Auth.Domain.Tokens;

public class RefreshToken
{
    private static readonly TimeSpan Lifetime = TimeSpan.FromDays(7);

    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public Guid FamilyId { get; private set; }
    public string TokenHash { get; private set; } = string.Empty;
    public DateTime ExpiresAt { get; private set; }
    public DateTime? UsedAt { get; private set; }
    public DateTime? RevokedAt { get; private set; }
    public DateTime CreatedAt { get; private set; }

    public bool IsUsed => UsedAt is not null;
    public bool IsRevoked => RevokedAt is not null;

    private RefreshToken()
    {
    }

    public static RefreshToken Issue(Guid userId, Guid familyId, string tokenHash, DateTime now)
    {
        return new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            FamilyId = familyId,
            TokenHash = tokenHash,
            ExpiresAt = now + Lifetime,
            CreatedAt = now
        };
    }

    public bool IsExpired(DateTime now)
    {
        return ExpiresAt <= now;
    }

    public void MarkUsed(DateTime now)
    {
        UsedAt = now;
    }

    public void Revoke(DateTime now)
    {
        RevokedAt ??= now;
    }
}
