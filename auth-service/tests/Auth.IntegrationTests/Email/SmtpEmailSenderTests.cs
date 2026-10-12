using Auth.Infrastructure.Email;
using MailKit.Security;
using MimeKit;

namespace Auth.IntegrationTests.Email;

public class SmtpEmailSenderTests
{
    private readonly SmtpOptions _options = new()
    {
        Host = "smtp.example.com",
        FromAddress = "no-reply@example.com",
        FromName = "Product System"
    };

    [Fact]
    public void BuildMessage_SetsFromToSubjectAndPlainTextBody()
    {
        var message = SmtpEmailSender.BuildMessage(_options, "user@example.com", "Reset your password", "Hello link");

        var from = Assert.Single(message.From.Mailboxes);
        Assert.Equal("no-reply@example.com", from.Address);
        Assert.Equal("Product System", from.Name);
        Assert.Equal("user@example.com", Assert.Single(message.To.Mailboxes).Address);
        Assert.Equal("Reset your password", message.Subject);
        Assert.Equal("Hello link", message.TextBody);
    }

    [Theory]
    [InlineData("tls", SecureSocketOptions.StartTls)]
    [InlineData("TLS", SecureSocketOptions.StartTls)]
    [InlineData("starttls", SecureSocketOptions.StartTls)]
    [InlineData("ssl", SecureSocketOptions.SslOnConnect)]
    [InlineData("none", SecureSocketOptions.None)]
    [InlineData("", SecureSocketOptions.None)]
    public void SecurityFor_MapsCryptoSetting(string crypto, SecureSocketOptions expected)
    {
        Assert.Equal(expected, SmtpEmailSender.SecurityFor(crypto));
    }

    [Fact]
    public void SecurityFor_UnknownValue_Throws()
    {
        Assert.Throws<InvalidOperationException>(() => SmtpEmailSender.SecurityFor("rot13"));
    }

    [Fact]
    public void BuildMessage_InvalidRecipient_Throws()
    {
        Assert.Throws<ParseException>(() =>
            SmtpEmailSender.BuildMessage(_options, "not an email", "Subject", "Body"));
    }
}
