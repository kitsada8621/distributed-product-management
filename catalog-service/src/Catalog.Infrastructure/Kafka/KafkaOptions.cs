namespace Catalog.Infrastructure.Kafka;

public sealed class KafkaOptions
{
    public bool Enabled { get; set; } = true;

    public string BootstrapServers { get; set; } = "localhost:9092";

    public string GroupId { get; set; } = "catalog-service";

    public int MaxAttempts { get; set; } = 3;

    public int RetryDelayMs { get; set; } = 500;

    public int RelayIntervalSeconds { get; set; } = 1;
}
