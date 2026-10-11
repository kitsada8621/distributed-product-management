using Catalog.Application.Common;
using Catalog.Application.Events;
using Catalog.Domain.Messaging;

namespace Catalog.Application.Messaging;

public sealed record DeadLetterRequest(
    string OriginalTopic,
    string MessageKey,
    string OriginalMessage,
    string CorrelationId,
    string Error,
    int Attempts);

public sealed record DeadLetterPayload(string OriginalTopic, string OriginalMessage, string Error, int Attempts);

public sealed class SendToDeadLetter(IOutboxRepository outbox, IUnitOfWork unitOfWork, TimeProvider timeProvider)
{
    public async Task ExecuteAsync(DeadLetterRequest request, CancellationToken cancellationToken)
    {
        var payload = new DeadLetterPayload(
            request.OriginalTopic, request.OriginalMessage, request.Error, request.Attempts);

        var message = OutboxMessage.Create(
            Guid.NewGuid(),
            Topics.DeadLetter(request.OriginalTopic),
            request.MessageKey,
            EventJson.Serialize(payload),
            request.CorrelationId,
            timeProvider.GetUtcNow().UtcDateTime);

        await outbox.AddAsync(message, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
