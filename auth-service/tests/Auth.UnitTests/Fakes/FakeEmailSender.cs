using Auth.Application.Emails;

namespace Auth.UnitTests.Fakes;

public sealed class FakeEmailSender : IEmailSender
{
    public List<string> SentTo { get; } = [];

    public HashSet<string> FailFor { get; } = [];

    public Task SendAsync(string toEmail, string subject, string body, CancellationToken cancellationToken)
    {
        if (FailFor.Contains(toEmail))
        {
            throw new InvalidOperationException("smtp down");
        }

        SentTo.Add(toEmail);
        return Task.CompletedTask;
    }
}
