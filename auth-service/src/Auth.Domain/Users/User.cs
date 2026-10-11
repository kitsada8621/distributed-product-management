namespace Auth.Domain.Users;

public class User
{
    private const int MaxFailedAttempts = 5;
    private static readonly TimeSpan LockDuration = TimeSpan.FromMinutes(15);

    public Guid Id { get; private set; }
    public string Email { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public UserRole Role { get; private set; }
    public int FailedAttempts { get; private set; }
    public DateTime? LockedUntil { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private User()
    {
    }

    public static User Create(string email, string name, string passwordHash, UserRole role, DateTime now)
    {
        return new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            Name = name,
            PasswordHash = passwordHash,
            Role = role,
            FailedAttempts = 0,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public bool IsLocked(DateTime now)
    {
        return LockedUntil > now;
    }

    public void RecordFailedLogin(DateTime now)
    {
        FailedAttempts++;
        if (FailedAttempts >= MaxFailedAttempts)
        {
            LockedUntil = now + LockDuration;
            FailedAttempts = 0;
        }
        UpdatedAt = now;
    }

    public void RecordSuccessfulLogin(DateTime now)
    {
        FailedAttempts = 0;
        LockedUntil = null;
        UpdatedAt = now;
    }

    public void ChangePassword(string passwordHash, DateTime now)
    {
        PasswordHash = passwordHash;
        UpdatedAt = now;
    }
}
