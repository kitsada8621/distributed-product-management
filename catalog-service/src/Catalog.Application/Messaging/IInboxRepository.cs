using Catalog.Domain.Messaging;

namespace Catalog.Application.Messaging;

public interface IInboxRepository
{
    Task<bool> ExistsAsync(Guid eventId, CancellationToken cancellationToken);

    Task AddAsync(InboxMessage message, CancellationToken cancellationToken);
}
