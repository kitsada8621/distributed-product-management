using Auth.Application.Common;
using Auth.Application.Users;
using Auth.Domain.Tokens;
using Auth.Domain.Users;
using Auth.UnitTests.Fakes;

namespace Auth.UnitTests.Users;

public class ResetPasswordTests
{
    private readonly FakeUserRepository _users = new();
    private readonly FakeResetTokenRepository _resetTokens = new();
    private readonly FakeRefreshTokenRepository _refreshTokens = new();
    private readonly FakeSecureTokenService _secureTokens = new();
    private readonly FakeTimeProvider _time = new();
    private readonly User _user;
    private readonly ResetPassword _useCase;

    public ResetPasswordTests()
    {
        _user = User.Create("user@example.com", "Somchai", "hashed:OldPassw0rd", UserRole.User, Now());
        _users.Items.Add(_user);
        _useCase = new ResetPassword(
            _resetTokens, _refreshTokens, _users, new FakePasswordHasher(), _secureTokens, new FakeUnitOfWork(), _time);
    }

    [Fact]
    public async Task Reset_ValidToken_ChangesPasswordMarksTokenUsedAndRevokesOnlyThisUsersTokens()
    {
        var resetToken = AddResetToken("raw-reset");
        var mine = RefreshToken.Issue(_user.Id, Guid.NewGuid(), "hash:a", Now());
        var other = RefreshToken.Issue(Guid.NewGuid(), Guid.NewGuid(), "hash:b", Now());
        _refreshTokens.Items.AddRange([mine, other]);

        await _useCase.ExecuteAsync(new ResetPasswordRequest("raw-reset", "NewPassw0rd!"), CancellationToken.None);

        Assert.Equal("hashed:NewPassw0rd!", _user.PasswordHash);
        Assert.False(resetToken.IsUsable(Now()));
        Assert.True(mine.IsRevoked);
        Assert.False(other.IsRevoked);
    }

    [Fact]
    public async Task Reset_SameTokenTwice_ThrowsResetTokenInvalid()
    {
        AddResetToken("raw-reset");
        await _useCase.ExecuteAsync(new ResetPasswordRequest("raw-reset", "NewPassw0rd!"), CancellationToken.None);

        var exception = await Assert.ThrowsAsync<AuthException>(() =>
            _useCase.ExecuteAsync(new ResetPasswordRequest("raw-reset", "AnotherPassw0rd!"), CancellationToken.None));

        Assert.Equal(ErrorCodes.ResetTokenInvalid, exception.Code);
        Assert.Equal("hashed:NewPassw0rd!", _user.PasswordHash);
    }

    [Fact]
    public async Task Reset_ExpiredToken_ThrowsResetTokenInvalid()
    {
        AddResetToken("raw-reset");
        _time.UtcNow = _time.UtcNow.AddMinutes(31);

        var exception = await Assert.ThrowsAsync<AuthException>(() =>
            _useCase.ExecuteAsync(new ResetPasswordRequest("raw-reset", "NewPassw0rd!"), CancellationToken.None));

        Assert.Equal(ErrorCodes.ResetTokenInvalid, exception.Code);
    }

    [Theory]
    [InlineData("unknown")]
    [InlineData("")]
    [InlineData(null)]
    public async Task Reset_UnknownOrMissingToken_ThrowsResetTokenInvalid(string? token)
    {
        AddResetToken("raw-reset");

        var exception = await Assert.ThrowsAsync<AuthException>(() =>
            _useCase.ExecuteAsync(new ResetPasswordRequest(token, "NewPassw0rd!"), CancellationToken.None));

        Assert.Equal(ErrorCodes.ResetTokenInvalid, exception.Code);
    }

    [Theory]
    [InlineData("short", ErrorCodes.TooShort)]
    [InlineData("", ErrorCodes.Required)]
    [InlineData(null, ErrorCodes.Required)]
    public async Task Reset_InvalidPassword_ThrowsValidationErrorAndKeepsTokenUsable(string? password, string fieldCode)
    {
        var resetToken = AddResetToken("raw-reset");

        var exception = await Assert.ThrowsAsync<AuthException>(() =>
            _useCase.ExecuteAsync(new ResetPasswordRequest("raw-reset", password), CancellationToken.None));

        Assert.Equal(ErrorCodes.ValidationError, exception.Code);
        var error = Assert.Single(exception.Errors!);
        Assert.Equal("newPassword", error.Field);
        Assert.Equal(fieldCode, error.Code);
        Assert.True(resetToken.IsUsable(Now()));
    }

    private DateTime Now() => _time.UtcNow.UtcDateTime;

    private ResetToken AddResetToken(string raw)
    {
        var token = ResetToken.Issue(_user.Id, _secureTokens.Hash(raw), Now());
        _resetTokens.Items.Add(token);
        return token;
    }
}
