using Auth.Application.Common;
using Auth.Application.Tokens;
using Auth.Domain.Tokens;
using Auth.UnitTests.Fakes;

namespace Auth.UnitTests.Tokens;

public class LogoutSessionTests
{
    private readonly FakeRefreshTokenRepository _refreshTokens = new();
    private readonly FakeSecureTokenService _secureTokens = new();
    private readonly FakeTimeProvider _time = new();
    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _familyId = Guid.NewGuid();
    private readonly LogoutSession _useCase;

    public LogoutSessionTests()
    {
        _useCase = new LogoutSession(_refreshTokens, _secureTokens, new FakeUnitOfWork(), _time);
    }

    [Fact]
    public async Task Logout_ValidRequest_RevokesWholeFamilyIncludingUsedTokens()
    {
        var rotated = AddToken("raw-1");
        rotated.MarkUsed(_time.UtcNow.UtcDateTime);
        AddToken("raw-2");
        var otherFamily = RefreshToken.Issue(_userId, Guid.NewGuid(), "hash:other", _time.UtcNow.UtcDateTime);
        _refreshTokens.Items.Add(otherFamily);

        await _useCase.ExecuteAsync(ValidRequest("raw-2"), CancellationToken.None);

        Assert.True(_refreshTokens.Items[0].IsRevoked);
        Assert.True(_refreshTokens.Items[1].IsRevoked);
        Assert.False(otherFamily.IsRevoked);
    }

    [Fact]
    public async Task Logout_TokenOfAnotherUser_ThrowsCsrfInvalidAndRevokesNothing()
    {
        var token = AddToken("raw-1");

        var request = ValidRequest("raw-1") with { UserId = Guid.NewGuid() };
        var exception = await Assert.ThrowsAsync<AuthException>(() => _useCase.ExecuteAsync(request, CancellationToken.None));

        Assert.Equal(ErrorCodes.CsrfInvalid, exception.Code);
        Assert.False(token.IsRevoked);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("unknown")]
    public async Task Logout_MissingOrUnknownRefreshToken_ThrowsCsrfInvalid(string? rawToken)
    {
        AddToken("raw-1");

        var exception = await Assert.ThrowsAsync<AuthException>(() =>
            _useCase.ExecuteAsync(ValidRequest(rawToken), CancellationToken.None));

        Assert.Equal(ErrorCodes.CsrfInvalid, exception.Code);
    }

    [Fact]
    public async Task Logout_BadCsrf_ThrowsCsrfInvalidAndRevokesNothing()
    {
        var token = AddToken("raw-1");
        var request = new LogoutRequest(_userId, "raw-1", "bad", "bad");

        var exception = await Assert.ThrowsAsync<AuthException>(() => _useCase.ExecuteAsync(request, CancellationToken.None));

        Assert.Equal(ErrorCodes.CsrfInvalid, exception.Code);
        Assert.False(token.IsRevoked);
    }

    [Fact]
    public async Task Logout_SecondTime_IsIdempotent()
    {
        var token = AddToken("raw-1");
        await _useCase.ExecuteAsync(ValidRequest("raw-1"), CancellationToken.None);
        var firstRevokedAt = token.RevokedAt;
        _time.UtcNow = _time.UtcNow.AddMinutes(5);

        await _useCase.ExecuteAsync(ValidRequest("raw-1"), CancellationToken.None);

        Assert.Equal(firstRevokedAt, token.RevokedAt);
    }

    private RefreshToken AddToken(string raw)
    {
        var token = RefreshToken.Issue(_userId, _familyId, _secureTokens.Hash(raw), _time.UtcNow.UtcDateTime);
        _refreshTokens.Items.Add(token);
        return token;
    }

    private LogoutRequest ValidRequest(string? rawToken)
    {
        var csrf = _secureTokens.CreateCsrfToken(_familyId);
        return new LogoutRequest(_userId, rawToken, csrf, csrf);
    }
}
