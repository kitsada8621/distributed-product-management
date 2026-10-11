namespace Auth.Domain.Emails;

public class EmailOutbox
{
    private const int MaxAttempts = 5;
    private static readonly TimeSpan BaseBackoff = TimeSpan.FromSeconds(30);

    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public string ToEmail { get; private set; } = string.Empty;
    public string Subject { get; private set; } = string.Empty;
    public string Body { get; private set; } = string.Empty;
    public EmailStatus Status { get; private set; }
    public int Attempts { get; private set; }
    public DateTime NextAttemptAt { get; private set; }
    public string? LastError { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? SentAt { get; private set; }

    private EmailOutbox()
    {
    }

    public static EmailOutbox Create(Guid userId, string toEmail, string subject, string body, DateTime now)
    {
        return new EmailOutbox
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            ToEmail = toEmail,
            Subject = subject,
            Body = body,
            Status = EmailStatus.Pending,
            Attempts = 0,
            NextAttemptAt = now,
            CreatedAt = now
        };
    }

    public void MarkSent(DateTime now)
    {
        Status = EmailStatus.Sent;
        SentAt = now;
    }

    public void MarkAttemptFailed(string error, DateTime now)
    {
        Attempts++;
        LastError = error;
        if (Attempts >= MaxAttempts)
        {
            Status = EmailStatus.Failed;
            return;
        }

        NextAttemptAt = now + BaseBackoff * (1 << (Attempts - 1));
    }
}
