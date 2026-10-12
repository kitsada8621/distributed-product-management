using Catalog.Application.Messaging;

namespace Catalog.UnitTests.Fakes;

public sealed record PublishedMessage(string Topic, string Key, string Payload, string CorrelationId);

public sealed class FakeEventPublisher : IEventPublisher
{
    public List<PublishedMessage> Published { get; } = [];

    public bool ShouldThrow { get; set; }

    public Task PublishAsync(string topic, string key, string payload, string correlationId, CancellationToken cancellationToken)
    {
        if (ShouldThrow)
        {
            throw new InvalidOperationException("Kafka did not acknowledge");
        }

        Published.Add(new PublishedMessage(topic, key, payload, correlationId));
        return Task.CompletedTask;
    }
}
