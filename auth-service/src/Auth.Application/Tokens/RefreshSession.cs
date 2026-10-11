using Auth.Application.Common;
using Auth.Application.Security;
using Auth.Application.Users;
using Auth.Domain.Tokens;

namespace Auth.Application.Tokens;

public sealed record RefreshRequest(string? RefreshToken, string? CsrfHeader, string? CsrfCookie);

public sealed class RefreshSession(
    IRefreshTokenRepository refreshTokens,
    IUserRepository users,
    IAccessTokenIssuer accessTokenIssuer,
    ISecureTokenService secureTokens,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
{
    public async Task<LoginResult> ExecuteAsync(RefreshRequest request, CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;

        var current = await FindTokenAsync(request.RefreshToken, cancellationToken);
        EnsureValidCsrf(request, current.FamilyId);

        if (current.IsUsed)
        {
            await RevokeFamilyAsync(current.FamilyId, now, cancellationToken);
            throw new AuthException(ErrorCodes.RefreshReused, "Refresh token was already used");
        }

        if (current.IsRevoked || current.IsExpired(now))
        {
            throw RefreshInvalid();
        }

        var user = await users.GetByIdAsync(current.UserId, cancellationToken) ?? throw RefreshInvalid();

        current.MarkUsed(now);
        var rawRefreshToken = secureTokens.GenerateOpaqueToken();
        var next = RefreshToken.Issue(user.Id, current.FamilyId, secureTokens.Hash(rawRefreshToken), now);
        await refreshTokens.AddAsync(next, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new LoginResult(
            accessTokenIssuer.Issue(user),
            rawRefreshToken,
            secureTokens.CreateCsrfToken(current.FamilyId));
    }

    private async Task<RefreshToken> FindTokenAsync(string? rawToken, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(rawToken))
        {
            throw RefreshInvalid();
        }

        var token = await refreshTokens.GetByHashAsync(secureTokens.Hash(rawToken), cancellationToken);
        return token ?? throw RefreshInvalid();
    }

    private void EnsureValidCsrf(RefreshRequest request, Guid familyId)
    {
        var header = request.CsrfHeader;
        var isValid = !string.IsNullOrEmpty(header)
            && header == request.CsrfCookie
            && secureTokens.IsValidCsrfToken(header, familyId);
        if (!isValid)
        {
            throw new AuthException(ErrorCodes.CsrfInvalid, "Invalid CSRF token");
        }
    }

    private async Task RevokeFamilyAsync(Guid familyId, DateTime now, CancellationToken cancellationToken)
    {
        var family = await refreshTokens.GetFamilyAsync(familyId, cancellationToken);
        foreach (var token in family)
        {
            token.Revoke(now);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private static AuthException RefreshInvalid()
    {
        return new AuthException(ErrorCodes.RefreshInvalid, "Refresh token is invalid");
    }
}
