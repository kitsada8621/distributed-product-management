using Auth.Application.Common;
using Auth.Application.Security;
using Auth.Application.Tokens;
using Auth.Domain.Tokens;
using Auth.Domain.Users;

namespace Auth.Application.Users;

public sealed record LoginRequest(string? Email, string? Password);

public sealed record LoginResult(string AccessToken, string RefreshToken, string CsrfToken);

public sealed record AccountLockedData(DateTime LockedUntil);

public sealed class LoginUser(
    IUserRepository users,
    IPasswordHasher passwordHasher,
    IAccessTokenIssuer accessTokenIssuer,
    ISecureTokenService secureTokens,
    IRefreshTokenRepository refreshTokens,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
{
    public async Task<LoginResult> ExecuteAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        var email = request.Email?.Trim() ?? string.Empty;
        var password = request.Password ?? string.Empty;
        ValidateRequired(email, password);

        var now = timeProvider.GetUtcNow().UtcDateTime;
        var user = await users.GetByEmailAsync(email, cancellationToken);
        if (user is null)
        {
            throw InvalidCredentials();
        }

        if (user.IsLocked(now))
        {
            throw AccountLocked(user);
        }

        if (!passwordHasher.Verify(password, user.PasswordHash))
        {
            await RecordFailureAsync(user, now, cancellationToken);
            throw user.IsLocked(now) ? AccountLocked(user) : InvalidCredentials();
        }

        return await CompleteLoginAsync(user, now, cancellationToken);
    }

    private async Task RecordFailureAsync(User user, DateTime now, CancellationToken cancellationToken)
    {
        user.RecordFailedLogin(now);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task<LoginResult> CompleteLoginAsync(User user, DateTime now, CancellationToken cancellationToken)
    {
        user.RecordSuccessfulLogin(now);

        var familyId = Guid.NewGuid();
        var rawRefreshToken = secureTokens.GenerateOpaqueToken();
        var refreshToken = RefreshToken.Issue(user.Id, familyId, secureTokens.Hash(rawRefreshToken), now);
        await refreshTokens.AddAsync(refreshToken, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new LoginResult(
            accessTokenIssuer.Issue(user),
            rawRefreshToken,
            secureTokens.CreateCsrfToken(familyId));
    }

    private static void ValidateRequired(string email, string password)
    {
        var errors = new List<FieldError>();
        if (email.Length == 0)
        {
            errors.Add(new FieldError("email", ErrorCodes.Required, "Email is required"));
        }

        if (password.Length == 0)
        {
            errors.Add(new FieldError("password", ErrorCodes.Required, "Password is required"));
        }

        if (errors.Count > 0)
        {
            throw new AuthException(ErrorCodes.ValidationError, "Invalid input", errors);
        }
    }

    private static AuthException InvalidCredentials()
    {
        return new AuthException(ErrorCodes.InvalidCredentials, "Invalid email or password");
    }

    private static AuthException AccountLocked(User user)
    {
        return new AuthException(
            ErrorCodes.AccountLocked,
            "Account is locked. Try again later",
            data: new AccountLockedData(user.LockedUntil!.Value));
    }
}
