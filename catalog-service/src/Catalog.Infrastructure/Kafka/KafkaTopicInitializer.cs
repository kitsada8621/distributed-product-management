using Catalog.Application.Events;
using Confluent.Kafka;
using Confluent.Kafka.Admin;

namespace Catalog.Infrastructure.Kafka;

public static class KafkaTopicInitializer
{
    private static readonly string[] CatalogTopics =
    [
        Topics.OrderCreated,
        Topics.OrderConfirmed,
        Topics.OrderCancelled,
        Topics.StockReserved,
        Topics.StockRejected
    ];

    // One partition, replication factor 1 (A-11, dev setup).
    public static async Task EnsureAsync(KafkaOptions options, CancellationToken cancellationToken)
    {
        var config = new AdminClientConfig { BootstrapServers = options.BootstrapServers };
        using var admin = new AdminClientBuilder(config).Build();

        var specifications = CatalogTopics
            .Select(topic => new TopicSpecification { Name = topic, NumPartitions = 1, ReplicationFactor = 1 })
            .ToList();

        try
        {
            await admin.CreateTopicsAsync(specifications);
        }
        catch (CreateTopicsException exception) when (AllAlreadyExist(exception))
        {
            // Topics from an earlier start are fine.
        }
    }

    private static bool AllAlreadyExist(CreateTopicsException exception)
    {
        return exception.Results.All(result =>
            result.Error.Code == ErrorCode.NoError || result.Error.Code == ErrorCode.TopicAlreadyExists);
    }
}
