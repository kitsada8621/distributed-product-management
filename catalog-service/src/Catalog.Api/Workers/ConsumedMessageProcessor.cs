using Catalog.Application.Events;
using Catalog.Application.Messaging;
using Catalog.Application.Reservations;
using Catalog.Infrastructure.Kafka;

namespace Catalog.Api.Workers;

public sealed class ConsumedMessageProcessor(
    IServiceScopeFactory scopes,
    KafkaOptions options,
    ILogger<ConsumedMessageProcessor> logger)
{
    // Never throws for a bad message, so the consumer can move on to the next one.
    public async Task ProcessAsync(ConsumedMessage message, CancellationToken cancellationToken)
    {
        using var logScope = logger.BeginScope(new Dictionary<string, object> { ["correlationId"] = message.CorrelationId });

        Exception? lastFailure = null;
        for (var attempt = 1; attempt <= options.MaxAttempts; attempt++)
        {
            try
            {
                var outcome = await HandleAsync(message, cancellationToken);
                LogOutcome(message, outcome);
                return;
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                lastFailure = exception;
                logger.LogWarning(exception, "Attempt {Attempt} of {Max} failed for {Topic}", attempt, options.MaxAttempts, message.Topic);
                await WaitBeforeRetryAsync(attempt, cancellationToken);
            }
        }

        await DeadLetterAsync(message, lastFailure!, cancellationToken);
    }

    private async Task<EventOutcome> HandleAsync(ConsumedMessage message, CancellationToken cancellationToken)
    {
        await using var scope = scopes.CreateAsyncScope();
        var services = scope.ServiceProvider;

        switch (message.Topic)
        {
            case Topics.OrderCreated:
                var created = EventJson.Deserialize<OrderCreated>(message.Value);
                return await services.GetRequiredService<ReserveStock>()
                    .ExecuteAsync(created, message.CorrelationId, cancellationToken);
            case Topics.OrderConfirmed:
                var confirmed = EventJson.Deserialize<OrderConfirmed>(message.Value);
                return await services.GetRequiredService<CommitReservation>()
                    .ExecuteAsync(confirmed, message.CorrelationId, cancellationToken);
            case Topics.OrderCancelled:
                var cancelled = EventJson.Deserialize<OrderCancelled>(message.Value);
                return await services.GetRequiredService<ReleaseReservation>()
                    .ExecuteAsync(cancelled, message.CorrelationId, cancellationToken);
            default:
                throw new InvalidOperationException($"No handler for topic {message.Topic}");
        }
    }

    private async Task WaitBeforeRetryAsync(int attempt, CancellationToken cancellationToken)
    {
        if (attempt < options.MaxAttempts)
        {
            await Task.Delay(options.RetryDelayMs, cancellationToken);
        }
    }

    private async Task DeadLetterAsync(ConsumedMessage message, Exception failure, CancellationToken cancellationToken)
    {
        await using var scope = scopes.CreateAsyncScope();
        var request = new DeadLetterRequest(
            message.Topic, message.Key, message.Value, message.CorrelationId, failure.Message, options.MaxAttempts);
        await scope.ServiceProvider.GetRequiredService<SendToDeadLetter>().ExecuteAsync(request, cancellationToken);

        logger.LogError(
            "Message on {Topic} sent to dead letter after {Attempts} attempts, correlationId {CorrelationId}: {Error}",
            message.Topic, options.MaxAttempts, message.CorrelationId, failure.Message);
    }

    private void LogOutcome(ConsumedMessage message, EventOutcome outcome)
    {
        if (outcome == EventOutcome.Applied)
        {
            logger.LogInformation("Applied {Topic} for key {Key}", message.Topic, message.Key);
            return;
        }

        logger.LogInformation("{Outcome} {Topic} for key {Key}", outcome, message.Topic, message.Key);
    }
}
