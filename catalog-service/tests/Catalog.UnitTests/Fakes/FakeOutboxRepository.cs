using Catalog.Application.Messaging;
using Catalog.Domain.Messaging;

namespace Catalog.UnitTests.Fakes;

public sealed class FakeOutboxRepository : IOutboxRepository
{
    public List<OutboxMessage> Items { get; } = [];

    public Task AddAsync(OutboxMessage message, CancellationToken cancellationToken)
    {
        Items.Add(message);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<OutboxMessage>> ListDueAsync(DateTime now, int limit, CancellationToken cancellationToken)
    {
        IReadOnlyList<OutboxMessage> due = Items
            .Where(message => message.Status == OutboxStatus.Pending && message.NextAttemptAt <= now)
            .OrderBy(message => message.CreatedAt)
            .Take(limit)
            .ToList();
        return Task.FromResult(due);
    }
}
