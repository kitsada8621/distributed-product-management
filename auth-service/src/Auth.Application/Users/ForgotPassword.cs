using Auth.Application.Common;
using Auth.Application.Emails;
using Auth.Application.Security;
using Auth.Application.Tokens;
using Auth.Domain.Emails;
using Auth.Domain.Tokens;

namespace Auth.Application.Users;

public sealed class ForgotPassword(
    IUserRepository users,
    IResetTokenRepository resetTokens,
    IEmailOutboxRepository emailOutbox,
    ISecureTokenService secureTokens,
    ResetLinkOptions linkOptions,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
{
    public async Task ExecuteAsync(string? email, CancellationToken cancellationToken)
    {
        var address = email?.Trim() ?? string.Empty;
        Validate(address);

        var user = await users.GetByEmailAsync(address, cancellationToken);
        if (user is null)
        {
            return;
        }

        var now = timeProvider.GetUtcNow().UtcDateTime;
        var rawToken = secureTokens.GenerateOpaqueToken();
        await resetTokens.AddAsync(ResetToken.Issue(user.Id, secureTokens.Hash(rawToken), now), cancellationToken);

        var body = $"Use the link below to reset your password. The link expires in 30 minutes.\n\n"
            + $"{linkOptions.BaseUrl}?token={Uri.EscapeDataString(rawToken)}";
        await emailOutbox.AddAsync(EmailOutbox.Create(user.Id, user.Email, "Reset your password", body, now), cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private static void Validate(string email)
    {
        if (email.Length == 0)
        {
            throw EmailError(ErrorCodes.Required, "Email is required");
        }

        var at = email.IndexOf('@');
        var hasShape = at > 0 && at < email.Length - 1 && !email.Contains(' ');
        if (!hasShape)
        {
            throw EmailError(ErrorCodes.InvalidFormat, "Invalid email format");
        }
    }

    private static AuthException EmailError(string code, string message)
    {
        return new AuthException(ErrorCodes.ValidationError, "Invalid input", [new FieldError("email", code, message)]);
    }
}
