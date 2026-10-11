using Auth.Application.Emails;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace Auth.Infrastructure.Email;

public sealed class SmtpEmailSender(SmtpOptions options) : IEmailSender
{
    public static MimeMessage BuildMessage(SmtpOptions options, string toEmail, string subject, string body)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(options.FromName, options.FromAddress));
        message.To.Add(MailboxAddress.Parse(toEmail));
        message.Subject = subject;
        message.Body = new TextPart("plain") { Text = body };
        return message;
    }

    public static SecureSocketOptions SecurityFor(string crypto)
    {
        return crypto.Trim().ToLowerInvariant() switch
        {
            "tls" or "starttls" => SecureSocketOptions.StartTls,
            "ssl" => SecureSocketOptions.SslOnConnect,
            "none" or "" => SecureSocketOptions.None,
            _ => throw new InvalidOperationException($"Smtp:Crypto must be tls, ssl or none, but was '{crypto}'")
        };
    }

    public async Task SendAsync(string toEmail, string subject, string body, CancellationToken cancellationToken)
    {
        var message = BuildMessage(options, toEmail, subject, body);

        using var client = new SmtpClient();
        await client.ConnectAsync(options.Host, options.Port, SecurityFor(options.Crypto), cancellationToken);
        if (!string.IsNullOrEmpty(options.Username))
        {
            await client.AuthenticateAsync(options.Username, options.Password, cancellationToken);
        }

        await client.SendAsync(message, cancellationToken);
        await client.DisconnectAsync(true, cancellationToken);
    }
}
