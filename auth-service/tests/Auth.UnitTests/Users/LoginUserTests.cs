using Auth.Application.Common;
using Auth.Application.Users;
using Auth.Domain.Users;
using Auth.UnitTests.Fakes;

namespace Auth.UnitTests.Users;

public class LoginUserTests
{
    private const string Email = "user@example.com";
    private const string Password = "Passw0rd!";

    private readonly FakeUserRepository _users = new();
    private readonly FakeRefreshTokenRepository _refreshTokens = new();
    private readonly FakeUnitOfWork _unitOfWork = new();
    private readonly FakeTimeProvider _time = new();
    private readonly User _user;
    private readonly LoginUser _useCase;

    public LoginUserTests()
    {
        var hasher = new FakePasswordHasher();
        _user = User.Create(Email, "Somchai", hasher.Hash(Password), UserRole.User, _time.UtcNow.UtcDateTime);
        _users.Items.Add(_user);
        _useCase = new LoginUser(
            _users, hasher, new FakeAccessTokenIssuer(), new FakeSecureTokenService(), _refreshTokens, _unitOfWork, _time);
    }

    [Fact]
    public async Task Login_CorrectPassword_ReturnsTokensAndStoresOnlyHash()
    {
        var result = await _useCase.ExecuteAsync(new LoginRequest(Email, Password), CancellationToken.None);

        Assert.Equal("access:" + _user.Id, result.AccessToken);
        var stored = Assert.Single(_refreshTokens.Items);
        Assert.Equal(_user.Id, stored.UserId);
        Assert.Equal("hash:" + result.RefreshToken, stored.TokenHash);
        Assert.NotEqual(result.RefreshToken, stored.TokenHash);
        Assert.Equal("csrf:" + stored.FamilyId, result.CsrfToken);
    }

    [Fact]
    public async Task Login_Success_ResetsFailedCounter()
    {
        await FailLoginAsync(3);

        await _useCase.ExecuteAsync(new LoginRequest(Email, Password), CancellationToken.None);

        Assert.Equal(0, _user.FailedAttempts);
    }

    [Fact]
    public async Task Login_UnknownEmailAndWrongPassword_GiveIdenticalError()
    {
        var unknown = await Assert.ThrowsAsync<AuthException>(() =>
            _useCase.ExecuteAsync(new LoginRequest("nobody@example.com", Password), CancellationToken.None));
        var wrongPassword = await Assert.ThrowsAsync<AuthException>(() =>
            _useCase.ExecuteAsync(new LoginRequest(Email, "wrong-password"), CancellationToken.None));

        Assert.Equal(ErrorCodes.InvalidCredentials, unknown.Code);
        Assert.Equal(unknown.Code, wrongPassword.Code);
        Assert.Equal(unknown.Message, wrongPassword.Message);
    }

    [Fact]
    public async Task Login_FifthWrongPassword_ReturnsAccountLockedWithLockedUntil()
    {
        await FailLoginAsync(4);

        var exception = await Assert.ThrowsAsync<AuthException>(() =>
            _useCase.ExecuteAsync(new LoginRequest(Email, "wrong-password"), CancellationToken.None));

        Assert.Equal(ErrorCodes.AccountLocked, exception.Code);
        var data = Assert.IsType<AccountLockedData>(exception.Data);
        Assert.Equal(_time.UtcNow.UtcDateTime.AddMinutes(15), data.LockedUntil);
    }

    [Fact]
    public async Task Login_WhileLocked_FailsEvenWithCorrectPassword()
    {
        await FailLoginAsync(5);

        var exception = await Assert.ThrowsAsync<AuthException>(() =>
            _useCase.ExecuteAsync(new LoginRequest(Email, Password), CancellationToken.None));

        Assert.Equal(ErrorCodes.AccountLocked, exception.Code);
        Assert.Empty(_refreshTokens.Items);
    }

    [Fact]
    public async Task Login_AfterLockExpires_Succeeds()
    {
        await FailLoginAsync(5);
        _time.UtcNow = _time.UtcNow.AddMinutes(15);

        var result = await _useCase.ExecuteAsync(new LoginRequest(Email, Password), CancellationToken.None);

        Assert.NotEmpty(result.AccessToken);
    }

    [Theory]
    [InlineData("", Password, "email")]
    [InlineData(Email, "", "password")]
    public async Task Login_EmptyField_ThrowsValidationError(string email, string password, string field)
    {
        var exception = await Assert.ThrowsAsync<AuthException>(() =>
            _useCase.ExecuteAsync(new LoginRequest(email, password), CancellationToken.None));

        Assert.Equal(ErrorCodes.ValidationError, exception.Code);
        var error = Assert.Single(exception.Errors!);
        Assert.Equal(field, error.Field);
        Assert.Equal(ErrorCodes.Required, error.Code);
    }

    private async Task FailLoginAsync(int times)
    {
        for (var i = 0; i < times; i++)
        {
            await Assert.ThrowsAsync<AuthException>(() =>
                _useCase.ExecuteAsync(new LoginRequest(Email, "wrong-password"), CancellationToken.None));
        }
    }
}
