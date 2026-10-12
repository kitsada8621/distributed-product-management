using Auth.Domain.Emails;

namespace Auth.UnitTests.Domain;

public class EmailOutboxTests
{
    private static readonly DateTime Now = new(2026, 10, 10, 8, 30, 0, DateTimeKind.Utc);

    private static EmailOutbox NewEmail() => EmailOutbox.Create(Guid.NewGuid(), "user@example.com", "Subject", "Body", Now);

    [Fact]
    public void Create_StartsPendingAndDueNow()
    {
        var email = NewEmail();

        Assert.Equal(EmailStatus.Pending, email.Status);
        Assert.Equal(0, email.Attempts);
        Assert.Equal(Now, email.NextAttemptAt);
    }

    [Fact]
    public void MarkSent_SetsStatusAndSentAt()
    {
        var email = NewEmail();

        email.MarkSent(Now.AddSeconds(5));

        Assert.Equal(EmailStatus.Sent, email.Status);
        Assert.Equal(Now.AddSeconds(5), email.SentAt);
    }

    [Theory]
    [InlineData(1, 30)]
    [InlineData(2, 60)]
    [InlineData(3, 120)]
    [InlineData(4, 240)]
    public void MarkAttemptFailed_UsesExponentialBackoff(int failures, int expectedSeconds)
    {
        var email = NewEmail();

        for (var i = 0; i < failures; i++)
        {
            email.MarkAttemptFailed("smtp down", Now);
        }

        Assert.Equal(EmailStatus.Pending, email.Status);
        Assert.Equal(failures, email.Attempts);
        Assert.Equal(Now.AddSeconds(expectedSeconds), email.NextAttemptAt);
        Assert.Equal("smtp down", email.LastError);
    }

    [Fact]
    public void MarkAttemptFailed_FifthFailure_MarksFailed()
    {
        var email = NewEmail();

        for (var i = 0; i < 5; i++)
        {
            email.MarkAttemptFailed("smtp down", Now);
        }

        Assert.Equal(EmailStatus.Failed, email.Status);
        Assert.Equal(5, email.Attempts);
    }
}
