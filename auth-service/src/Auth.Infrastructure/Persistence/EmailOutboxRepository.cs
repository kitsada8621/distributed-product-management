using Auth.Application.Emails;
using Auth.Domain.Emails;
using Microsoft.EntityFrameworkCore;

namespace Auth.Infrastructure.Persistence;

public sealed class EmailOutboxRepository(AuthDbContext context) : IEmailOutboxRepository
{
    public async Task AddAsync(EmailOutbox email, CancellationToken cancellationToken)
    {
        await context.EmailOutboxes.AddAsync(email, cancellationToken);
    }

    public async Task<IReadOnlyList<EmailOutbox>> GetDueAsync(
        DateTime now, int batchSize, CancellationToken cancellationToken)
    {
        return await context.EmailOutboxes
            .Where(e => e.Status == EmailStatus.Pending && e.NextAttemptAt <= now)
            .OrderBy(e => e.NextAttemptAt)
            .Take(batchSize)
            .ToListAsync(cancellationToken);
    }
}
