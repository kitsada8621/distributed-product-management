using Auth.Application.Common;
using Auth.Application.Security;

namespace Auth.Application.Tokens;

public sealed record LogoutRequest(Guid UserId, string? RefreshToken, string? CsrfHeader, string? CsrfCookie);

public sealed class LogoutSession(
    IRefreshTokenRepository refreshTokens,
    ISecureTokenService secureTokens,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
{
    public async Task ExecuteAsync(LogoutRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(request.RefreshToken))
        {
            throw CsrfInvalid();
        }

        var current = await refreshTokens.GetByHashAsync(secureTokens.Hash(request.RefreshToken), cancellationToken);
        if (current is null || current.UserId != request.UserId)
        {
            throw CsrfInvalid();
        }

        var header = request.CsrfHeader;
        var isValidCsrf = !string.IsNullOrEmpty(header)
            && header == request.CsrfCookie
            && secureTokens.IsValidCsrfToken(header, current.FamilyId);
        if (!isValidCsrf)
        {
            throw CsrfInvalid();
        }

        var now = timeProvider.GetUtcNow().UtcDateTime;
        var family = await refreshTokens.GetFamilyAsync(current.FamilyId, cancellationToken);
        foreach (var token in family)
        {
            token.Revoke(now);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private static AuthException CsrfInvalid()
    {
        return new AuthException(ErrorCodes.CsrfInvalid, "Invalid CSRF token");
    }
}
