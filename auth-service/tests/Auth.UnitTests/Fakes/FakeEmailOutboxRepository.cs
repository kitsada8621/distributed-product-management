using Auth.Application.Emails;
using Auth.Domain.Emails;

namespace Auth.UnitTests.Fakes;

public sealed class FakeEmailOutboxRepository : IEmailOutboxRepository
{
    public List<EmailOutbox> Items { get; } = [];

    public Task AddAsync(EmailOutbox email, CancellationToken cancellationToken)
    {
        Items.Add(email);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<EmailOutbox>> GetDueAsync(DateTime now, int batchSize, CancellationToken cancellationToken)
    {
        IReadOnlyList<EmailOutbox> due = Items
            .Where(e => e.Status == EmailStatus.Pending && e.NextAttemptAt <= now)
            .OrderBy(e => e.NextAttemptAt)
            .Take(batchSize)
            .ToList();
        return Task.FromResult(due);
    }
}
