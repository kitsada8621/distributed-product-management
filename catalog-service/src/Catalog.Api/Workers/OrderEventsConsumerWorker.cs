using Catalog.Application.Events;
using Catalog.Infrastructure.Kafka;
using Confluent.Kafka;

namespace Catalog.Api.Workers;

public sealed class OrderEventsConsumerWorker(
    ConsumedMessageProcessor processor,
    KafkaOptions options,
    ILogger<OrderEventsConsumerWorker> logger) : BackgroundService
{
    private static readonly string[] OrderTopics = [Topics.OrderCreated, Topics.OrderConfirmed, Topics.OrderCancelled];

    private static readonly TimeSpan BrokerRetryDelay = TimeSpan.FromSeconds(5);

    // Consume() blocks, so the loop runs on its own thread and host startup is not held up.
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        return Task.Run(() => RunAsync(stoppingToken), stoppingToken);
    }

    private async Task RunAsync(CancellationToken stoppingToken)
    {
        try
        {
            await EnsureTopicsAsync(stoppingToken);

            using var consumer = BuildConsumer();
            consumer.Subscribe(OrderTopics);
            try
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    await ConsumeOneAsync(consumer, stoppingToken);
                }
            }
            finally
            {
                consumer.Close();
            }
        }
        catch (OperationCanceledException)
        {
            // Normal shutdown.
        }
    }

    private async Task EnsureTopicsAsync(CancellationToken stoppingToken)
    {
        while (true)
        {
            try
            {
                await KafkaTopicInitializer.EnsureAsync(options, stoppingToken);
                return;
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                logger.LogError(exception, "Kafka is not reachable yet, retrying in {Seconds} s", BrokerRetryDelay.TotalSeconds);
                await Task.Delay(BrokerRetryDelay, stoppingToken);
            }
        }
    }

    private IConsumer<string, string> BuildConsumer()
    {
        var config = new ConsumerConfig
        {
            BootstrapServers = options.BootstrapServers,
            GroupId = options.GroupId,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false
        };
        return new ConsumerBuilder<string, string>(config).Build();
    }

    private async Task ConsumeOneAsync(IConsumer<string, string> consumer, CancellationToken stoppingToken)
    {
        ConsumeResult<string, string> result;
        try
        {
            result = consumer.Consume(stoppingToken);
        }
        catch (ConsumeException exception)
        {
            logger.LogError(exception, "Kafka consume failed");
            return;
        }

        var correlationId = KafkaMessages.ReadCorrelationId(result.Message.Headers);
        var message = new ConsumedMessage(result.Topic, result.Message.Key, result.Message.Value, correlationId);

        await processor.ProcessAsync(message, stoppingToken);
        consumer.Commit(result);
    }
}
