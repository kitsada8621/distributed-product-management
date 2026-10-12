using Auth.Application.Emails;
using Auth.Domain.Emails;
using Auth.UnitTests.Fakes;

namespace Auth.UnitTests.Emails;

public class SendPendingEmailsTests
{
    private readonly FakeEmailOutboxRepository _outbox = new();
    private readonly FakeEmailSender _sender = new();
    private readonly FakeUnitOfWork _unitOfWork = new();
    private readonly FakeTimeProvider _time = new();
    private readonly SendPendingEmails _useCase;

    public SendPendingEmailsTests()
    {
        _useCase = new SendPendingEmails(_outbox, _sender, _unitOfWork, _time);
    }

    [Fact]
    public async Task Execute_DueEmail_IsSentAndMarkedSent()
    {
        var email = AddEmail("a@example.com");

        await _useCase.ExecuteAsync(CancellationToken.None);

        Assert.Equal(["a@example.com"], _sender.SentTo);
        Assert.Equal(EmailStatus.Sent, email.Status);
        Assert.Equal(_time.UtcNow.UtcDateTime, email.SentAt);
    }

    [Fact]
    public async Task Execute_SenderFails_KeepsPendingWithBackoffAndError()
    {
        var email = AddEmail("a@example.com");
        _sender.FailFor.Add("a@example.com");

        await _useCase.ExecuteAsync(CancellationToken.None);

        Assert.Equal(EmailStatus.Pending, email.Status);
        Assert.Equal(1, email.Attempts);
        Assert.Equal("smtp down", email.LastError);
        Assert.Equal(_time.UtcNow.UtcDateTime.AddSeconds(30), email.NextAttemptAt);
    }

    [Fact]
    public async Task Execute_FifthFailure_MarksFailedAndIsNeverSelectedAgain()
    {
        var email = AddEmail("a@example.com");
        _sender.FailFor.Add("a@example.com");

        for (var i = 0; i < 5; i++)
        {
            _time.UtcNow = _time.UtcNow.AddHours(1);
            await _useCase.ExecuteAsync(CancellationToken.None);
        }
        _sender.FailFor.Clear();
        _time.UtcNow = _time.UtcNow.AddHours(1);
        await _useCase.ExecuteAsync(CancellationToken.None);

        Assert.Equal(EmailStatus.Failed, email.Status);
        Assert.Equal(5, email.Attempts);
        Assert.Empty(_sender.SentTo);
    }

    [Fact]
    public async Task Execute_NotYetDueEmail_IsSkipped()
    {
        var email = AddEmail("a@example.com");
        email.MarkAttemptFailed("earlier failure", _time.UtcNow.UtcDateTime);

        await _useCase.ExecuteAsync(CancellationToken.None);

        Assert.Empty(_sender.SentTo);
        Assert.Equal(1, email.Attempts);
    }

    [Fact]
    public async Task Execute_OneFailingEmail_DoesNotBlockTheNext()
    {
        var failing = AddEmail("bad@example.com");
        var healthy = AddEmail("good@example.com");
        _sender.FailFor.Add("bad@example.com");

        await _useCase.ExecuteAsync(CancellationToken.None);

        Assert.Equal(EmailStatus.Pending, failing.Status);
        Assert.Equal(EmailStatus.Sent, healthy.Status);
        Assert.Equal(2, _unitOfWork.SaveCount);
    }

    private EmailOutbox AddEmail(string to)
    {
        var email = EmailOutbox.Create(Guid.NewGuid(), to, "Subject", "Body", _time.UtcNow.UtcDateTime);
        _outbox.Items.Add(email);
        return email;
    }
}
