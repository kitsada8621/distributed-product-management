using Auth.Application.Common;
using Auth.Application.Security;
using Auth.Application.Tokens;

namespace Auth.Application.Users;

public sealed record ResetPasswordRequest(string? Token, string? NewPassword);

public sealed class ResetPassword(
    IResetTokenRepository resetTokens,
    IRefreshTokenRepository refreshTokens,
    IUserRepository users,
    IPasswordHasher passwordHasher,
    ISecureTokenService secureTokens,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
{
    private const int MinPasswordLength = 8;

    public async Task ExecuteAsync(ResetPasswordRequest request, CancellationToken cancellationToken)
    {
        var newPassword = request.NewPassword ?? string.Empty;
        ValidatePassword(newPassword);

        if (string.IsNullOrEmpty(request.Token))
        {
            throw ResetTokenInvalid();
        }

        var now = timeProvider.GetUtcNow().UtcDateTime;
        var resetToken = await resetTokens.GetByHashAsync(secureTokens.Hash(request.Token), cancellationToken);
        if (resetToken is null || !resetToken.IsUsable(now))
        {
            throw ResetTokenInvalid();
        }

        var user = await users.GetByIdAsync(resetToken.UserId, cancellationToken) ?? throw ResetTokenInvalid();

        user.ChangePassword(passwordHasher.Hash(newPassword), now);
        resetToken.MarkUsed(now);

        var userTokens = await refreshTokens.GetByUserAsync(user.Id, cancellationToken);
        foreach (var token in userTokens)
        {
            token.Revoke(now);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private static void ValidatePassword(string newPassword)
    {
        if (newPassword.Length == 0)
        {
            throw PasswordError(ErrorCodes.Required, "New password is required");
        }

        if (newPassword.Length < MinPasswordLength)
        {
            throw PasswordError(ErrorCodes.TooShort, "Password must be at least 8 characters");
        }
    }

    private static AuthException PasswordError(string code, string message)
    {
        return new AuthException(
            ErrorCodes.ValidationError, "Invalid input", [new FieldError("newPassword", code, message)]);
    }

    private static AuthException ResetTokenInvalid()
    {
        return new AuthException(ErrorCodes.ResetTokenInvalid, "Reset token is invalid or expired");
    }
}
