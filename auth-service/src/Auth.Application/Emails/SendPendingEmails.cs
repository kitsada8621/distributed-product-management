using Auth.Application.Common;
using Auth.Domain.Emails;

namespace Auth.Application.Emails;

public sealed class SendPendingEmails(
    IEmailOutboxRepository outbox,
    IEmailSender emailSender,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
{
    private const int BatchSize = 20;

    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        var due = await outbox.GetDueAsync(timeProvider.GetUtcNow().UtcDateTime, BatchSize, cancellationToken);

        foreach (var email in due)
        {
            await SendOneAsync(email, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }

    private async Task SendOneAsync(EmailOutbox email, CancellationToken cancellationToken)
    {
        try
        {
            await emailSender.SendAsync(email.ToEmail, email.Subject, email.Body, cancellationToken);
            email.MarkSent(timeProvider.GetUtcNow().UtcDateTime);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            email.MarkAttemptFailed(exception.Message, timeProvider.GetUtcNow().UtcDateTime);
        }
    }
}
