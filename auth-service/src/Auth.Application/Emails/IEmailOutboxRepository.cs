using Auth.Domain.Emails;

namespace Auth.Application.Emails;

public interface IEmailOutboxRepository
{
    Task AddAsync(EmailOutbox email, CancellationToken cancellationToken);

    Task<IReadOnlyList<EmailOutbox>> GetDueAsync(DateTime now, int batchSize, CancellationToken cancellationToken);
}
