namespace Catalog.Application.Messaging;

public interface IEventPublisher
{
    // Throws when Kafka does not acknowledge the message.
    Task PublishAsync(string topic, string key, string payload, string correlationId, CancellationToken cancellationToken);
}
