using System.Collections.Concurrent;
using Auth.Application.Emails;

namespace Auth.IntegrationTests.Support;

public sealed record SentEmail(string To, string Subject, string Body);

public sealed class InMemoryEmailSender : IEmailSender
{
    private readonly ConcurrentQueue<SentEmail> _sent = new();

    public IReadOnlyCollection<SentEmail> Sent => _sent;

    public Task SendAsync(string toEmail, string subject, string body, CancellationToken cancellationToken)
    {
        _sent.Enqueue(new SentEmail(toEmail, subject, body));
        return Task.CompletedTask;
    }
}
