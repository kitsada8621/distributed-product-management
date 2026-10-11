using Catalog.Application.Messaging;
using Catalog.Domain.Messaging;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Infrastructure.Persistence;

public sealed class OutboxRepository(CatalogDbContext context) : IOutboxRepository
{
    public async Task AddAsync(OutboxMessage message, CancellationToken cancellationToken)
    {
        await context.OutboxMessages.AddAsync(message, cancellationToken);
    }

    public async Task<IReadOnlyList<OutboxMessage>> ListDueAsync(
        DateTime now,
        int limit,
        CancellationToken cancellationToken)
    {
        return await context.OutboxMessages
            .Where(m => m.Status == OutboxStatus.Pending && m.NextAttemptAt <= now)
            .OrderBy(m => m.NextAttemptAt)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }
}
