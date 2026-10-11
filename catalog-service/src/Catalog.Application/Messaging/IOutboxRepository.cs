using Catalog.Domain.Messaging;

namespace Catalog.Application.Messaging;

public interface IOutboxRepository
{
    Task AddAsync(OutboxMessage message, CancellationToken cancellationToken);

    // Tracked Pending messages whose NextAttemptAt has passed, oldest first.
    Task<IReadOnlyList<OutboxMessage>> ListDueAsync(DateTime now, int limit, CancellationToken cancellationToken);
}
