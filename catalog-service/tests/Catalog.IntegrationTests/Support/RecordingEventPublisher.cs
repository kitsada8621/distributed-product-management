using Catalog.Application.Messaging;

namespace Catalog.IntegrationTests.Support;

public sealed record RecordedMessage(string Topic, string Key, string Payload, string CorrelationId);

public sealed class RecordingEventPublisher : IEventPublisher
{
    public List<RecordedMessage> Published { get; } = [];

    // Number of upcoming PublishAsync calls that throw before publishing works again.
    public int FailuresRemaining { get; set; }

    public Task PublishAsync(string topic, string key, string payload, string correlationId, CancellationToken cancellationToken)
    {
        if (FailuresRemaining > 0)
        {
            FailuresRemaining--;
            throw new InvalidOperationException("Kafka did not acknowledge");
        }

        Published.Add(new RecordedMessage(topic, key, payload, correlationId));
        return Task.CompletedTask;
    }
}
