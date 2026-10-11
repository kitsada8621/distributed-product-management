namespace Catalog.Domain.Messaging;

public class OutboxMessage
{
    private static readonly TimeSpan BaseBackoff = TimeSpan.FromSeconds(1);
    private static readonly TimeSpan MaxBackoff = TimeSpan.FromSeconds(60);

    public Guid Id { get; private set; }
    public string Topic { get; private set; } = string.Empty;
    public string MessageKey { get; private set; } = string.Empty;
    public string Payload { get; private set; } = string.Empty;
    public string CorrelationId { get; private set; } = string.Empty;
    public OutboxStatus Status { get; private set; }
    public int Attempts { get; private set; }
    public DateTime NextAttemptAt { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? SentAt { get; private set; }

    private OutboxMessage()
    {
    }

    public static OutboxMessage Create(
        Guid id,
        string topic,
        string messageKey,
        string payload,
        string correlationId,
        DateTime now)
    {
        return new OutboxMessage
        {
            Id = id,
            Topic = topic,
            MessageKey = messageKey,
            Payload = payload,
            CorrelationId = correlationId,
            Status = OutboxStatus.Pending,
            Attempts = 0,
            NextAttemptAt = now,
            CreatedAt = now
        };
    }

    public void MarkSent(DateTime now)
    {
        Status = OutboxStatus.Sent;
        SentAt = now;
    }

    public void MarkFailed(DateTime now)
    {
        Attempts++;
        NextAttemptAt = now + CalculateBackoff();
    }

    private TimeSpan CalculateBackoff()
    {
        // Cap the exponent first so the shift cannot overflow for a long-failing message.
        var exponent = Math.Min(Attempts - 1, 6);
        var backoff = BaseBackoff * (1 << exponent);
        return backoff > MaxBackoff ? MaxBackoff : backoff;
    }
}
