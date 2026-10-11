using Catalog.Application.Messaging;
using Confluent.Kafka;

namespace Catalog.Infrastructure.Kafka;

public sealed class KafkaEventPublisher : IEventPublisher, IDisposable
{
    private readonly IProducer<string, string> _producer;

    public KafkaEventPublisher(KafkaOptions options)
    {
        var config = new ProducerConfig
        {
            BootstrapServers = options.BootstrapServers,
            Acks = Acks.All,
            EnableIdempotence = true
        };
        _producer = new ProducerBuilder<string, string>(config).Build();
    }

    // A ProduceException is left to propagate: the outbox relay turns it into a retry.
    public async Task PublishAsync(
        string topic,
        string key,
        string payload,
        string correlationId,
        CancellationToken cancellationToken)
    {
        var message = KafkaMessages.Create(key, payload, correlationId);
        await _producer.ProduceAsync(topic, message, cancellationToken);
    }

    public void Dispose()
    {
        _producer.Flush(TimeSpan.FromSeconds(5));
        _producer.Dispose();
    }
}
