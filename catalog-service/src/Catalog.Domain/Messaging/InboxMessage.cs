namespace Catalog.Domain.Messaging;

public class InboxMessage
{
    public Guid EventId { get; private set; }
    public string Topic { get; private set; } = string.Empty;
    public DateTime ProcessedAt { get; private set; }

    private InboxMessage()
    {
    }

    public static InboxMessage Create(Guid eventId, string topic, DateTime now)
    {
        return new InboxMessage
        {
            EventId = eventId,
            Topic = topic,
            ProcessedAt = now
        };
    }
}
