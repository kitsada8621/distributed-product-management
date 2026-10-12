using Catalog.Application.Messaging;
using Catalog.Domain.Messaging;

namespace Catalog.UnitTests.Fakes;

public sealed class FakeInboxRepository : IInboxRepository
{
    public List<InboxMessage> Items { get; } = [];

    public Task<bool> ExistsAsync(Guid eventId, CancellationToken cancellationToken)
    {
        return Task.FromResult(Items.Any(message => message.EventId == eventId));
    }

    public Task AddAsync(InboxMessage message, CancellationToken cancellationToken)
    {
        Items.Add(message);
        return Task.CompletedTask;
    }
}
