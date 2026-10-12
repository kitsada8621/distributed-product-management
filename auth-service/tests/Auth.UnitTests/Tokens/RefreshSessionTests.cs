using Auth.Application.Common;
using Auth.Application.Tokens;
using Auth.Domain.Tokens;
using Auth.Domain.Users;
using Auth.UnitTests.Fakes;

namespace Auth.UnitTests.Tokens;

public class RefreshSessionTests
{
    private readonly FakeUserRepository _users = new();
    private readonly FakeRefreshTokenRepository _refreshTokens = new();
    private readonly FakeSecureTokenService _secureTokens = new();
    private readonly FakeTimeProvider _time = new();
    private readonly User _user;
    private readonly Guid _familyId = Guid.NewGuid();
    private readonly RefreshSession _useCase;

    public RefreshSessionTests()
    {
        var now = _time.UtcNow.UtcDateTime;
        _user = User.Create("user@example.com", "Somchai", "hash", UserRole.User, now);
        _users.Items.Add(_user);
        _useCase = new RefreshSession(
            _refreshTokens, _users, new FakeAccessTokenIssuer(), _secureTokens, new FakeUnitOfWork(), _time);
    }

    [Fact]
    public async Task Refresh_ValidRequest_RotatesTokenInSameFamily()
    {
        var old = AddToken("raw-old");

        var result = await _useCase.ExecuteAsync(ValidRequest("raw-old"), CancellationToken.None);

        Assert.True(old.IsUsed);
        Assert.Equal(2, _refreshTokens.Items.Count);
        var next = _refreshTokens.Items[1];
        Assert.Equal(_familyId, next.FamilyId);
        Assert.Equal("hash:" + result.RefreshToken, next.TokenHash);
        Assert.NotEqual("raw-old", result.RefreshToken);
        Assert.Equal("access:" + _user.Id, result.AccessToken);
        Assert.Equal("csrf:" + _familyId, result.CsrfToken);
    }

    [Fact]
    public async Task Refresh_ReusedToken_RevokesWholeFamily()
    {
        AddToken("raw-old");
        var result = await _useCase.ExecuteAsync(ValidRequest("raw-old"), CancellationToken.None);

        var reuse = await Assert.ThrowsAsync<AuthException>(() =>
            _useCase.ExecuteAsync(ValidRequest("raw-old"), CancellationToken.None));

        Assert.Equal(ErrorCodes.RefreshReused, reuse.Code);
        Assert.All(_refreshTokens.Items, t => Assert.True(t.IsRevoked));
        var later = await Assert.ThrowsAsync<AuthException>(() =>
            _useCase.ExecuteAsync(ValidRequest(result.RefreshToken), CancellationToken.None));
        Assert.Equal(ErrorCodes.RefreshInvalid, later.Code);
    }

    [Theory]
    [InlineData(null, "csrf-cookie")]
    [InlineData("csrf-header", null)]
    [InlineData("csrf-header", "csrf-cookie")]
    public async Task Refresh_CsrfHeaderAndCookieDoNotMatch_ThrowsCsrfInvalid(string? header, string? cookie)
    {
        AddToken("raw-old");
        var request = new RefreshRequest("raw-old", header, cookie);

        var exception = await Assert.ThrowsAsync<AuthException>(() => _useCase.ExecuteAsync(request, CancellationToken.None));

        Assert.Equal(ErrorCodes.CsrfInvalid, exception.Code);
    }

    [Fact]
    public async Task Refresh_CsrfSignedForAnotherFamily_ThrowsCsrfInvalid()
    {
        AddToken("raw-old");
        var foreign = "csrf:" + Guid.NewGuid();
        var request = new RefreshRequest("raw-old", foreign, foreign);

        var exception = await Assert.ThrowsAsync<AuthException>(() => _useCase.ExecuteAsync(request, CancellationToken.None));

        Assert.Equal(ErrorCodes.CsrfInvalid, exception.Code);
    }

    [Fact]
    public async Task Refresh_BadCsrfOnUsedToken_DoesNotRevokeFamily()
    {
        var old = AddToken("raw-old");
        old.MarkUsed(_time.UtcNow.UtcDateTime);
        var request = new RefreshRequest("raw-old", "bad", "bad");

        var exception = await Assert.ThrowsAsync<AuthException>(() => _useCase.ExecuteAsync(request, CancellationToken.None));

        Assert.Equal(ErrorCodes.CsrfInvalid, exception.Code);
        Assert.False(old.IsRevoked);
    }

    [Fact]
    public async Task Refresh_RevokedToken_ThrowsRefreshInvalid()
    {
        AddToken("raw-old").Revoke(_time.UtcNow.UtcDateTime);

        var exception = await Assert.ThrowsAsync<AuthException>(() =>
            _useCase.ExecuteAsync(ValidRequest("raw-old"), CancellationToken.None));

        Assert.Equal(ErrorCodes.RefreshInvalid, exception.Code);
    }

    [Fact]
    public async Task Refresh_ExpiredToken_ThrowsRefreshInvalid()
    {
        AddToken("raw-old");
        _time.UtcNow = _time.UtcNow.AddDays(7);

        var exception = await Assert.ThrowsAsync<AuthException>(() =>
            _useCase.ExecuteAsync(ValidRequest("raw-old"), CancellationToken.None));

        Assert.Equal(ErrorCodes.RefreshInvalid, exception.Code);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("unknown")]
    public async Task Refresh_MissingOrUnknownToken_ThrowsRefreshInvalid(string? rawToken)
    {
        AddToken("raw-old");

        var exception = await Assert.ThrowsAsync<AuthException>(() =>
            _useCase.ExecuteAsync(ValidRequest(rawToken), CancellationToken.None));

        Assert.Equal(ErrorCodes.RefreshInvalid, exception.Code);
    }

    private RefreshToken AddToken(string raw)
    {
        var token = RefreshToken.Issue(_user.Id, _familyId, _secureTokens.Hash(raw), _time.UtcNow.UtcDateTime);
        _refreshTokens.Items.Add(token);
        return token;
    }

    private RefreshRequest ValidRequest(string? rawToken)
    {
        var csrf = _secureTokens.CreateCsrfToken(_familyId);
        return new RefreshRequest(rawToken, csrf, csrf);
    }
}
