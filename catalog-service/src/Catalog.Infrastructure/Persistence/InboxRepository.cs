using Catalog.Application.Messaging;
using Catalog.Domain.Messaging;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Infrastructure.Persistence;

public sealed class InboxRepository(CatalogDbContext context) : IInboxRepository
{
    public Task<bool> ExistsAsync(Guid eventId, CancellationToken cancellationToken)
    {
        return context.InboxMessages.AnyAsync(m => m.EventId == eventId, cancellationToken);
    }

    public async Task AddAsync(InboxMessage message, CancellationToken cancellationToken)
    {
        await context.InboxMessages.AddAsync(message, cancellationToken);
    }
}
