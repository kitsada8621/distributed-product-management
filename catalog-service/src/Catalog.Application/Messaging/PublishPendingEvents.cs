using Catalog.Application.Common;
using Catalog.Domain.Messaging;

namespace Catalog.Application.Messaging;

public sealed class PublishPendingEvents(
    IOutboxRepository outbox,
    IEventPublisher publisher,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
{
    private const int BatchSize = 50;

    // Returns how many messages were sent.
    public async Task<int> ExecuteAsync(CancellationToken cancellationToken)
    {
        var due = await outbox.ListDueAsync(timeProvider.GetUtcNow().UtcDateTime, BatchSize, cancellationToken);

        var sent = 0;
        foreach (var message in due)
        {
            if (await PublishOneAsync(message, cancellationToken))
            {
                sent++;
            }
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        return sent;
    }

    private async Task<bool> PublishOneAsync(OutboxMessage message, CancellationToken cancellationToken)
    {
        try
        {
            await publisher.PublishAsync(
                message.Topic, message.MessageKey, message.Payload, message.CorrelationId, cancellationToken);
            message.MarkSent(timeProvider.GetUtcNow().UtcDateTime);
            return true;
        }
        // API-SPEC §7.1: a failed publish is retried later with exponential backoff.
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            message.MarkFailed(timeProvider.GetUtcNow().UtcDateTime);
            return false;
        }
    }
}
