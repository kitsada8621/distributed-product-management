using Catalog.Application.Messaging;
using Catalog.Infrastructure.Kafka;

namespace Catalog.Api.Workers;

public sealed class OutboxRelayWorker(
    IServiceScopeFactory scopes,
    KafkaOptions options,
    ILogger<OutboxRelayWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await TickAsync(stoppingToken);
                await Task.Delay(TimeSpan.FromSeconds(options.RelayIntervalSeconds), stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            // Normal shutdown.
        }
    }

    public async Task RunOnceAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopes.CreateAsyncScope();
        var publishPendingEvents = scope.ServiceProvider.GetRequiredService<PublishPendingEvents>();
        var sent = await publishPendingEvents.ExecuteAsync(cancellationToken);
        if (sent > 0)
        {
            logger.LogInformation("Published {Count} outbox messages", sent);
        }
    }

    private async Task TickAsync(CancellationToken stoppingToken)
    {
        try
        {
            await RunOnceAsync(stoppingToken);
        }
        // A failed tick (for example the database is busy) must not stop the relay.
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogError(exception, "Outbox relay tick failed");
        }
    }
}
