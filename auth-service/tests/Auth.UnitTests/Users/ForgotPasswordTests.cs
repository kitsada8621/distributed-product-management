using Auth.Application.Common;
using Auth.Application.Users;
using Auth.Domain.Users;
using Auth.UnitTests.Fakes;

namespace Auth.UnitTests.Users;

public class ForgotPasswordTests
{
    private const string BaseUrl = "http://localhost:5173/reset-password";

    private readonly FakeUserRepository _users = new();
    private readonly FakeResetTokenRepository _resetTokens = new();
    private readonly FakeEmailOutboxRepository _outbox = new();
    private readonly FakeUnitOfWork _unitOfWork = new();
    private readonly FakeTimeProvider _time = new();
    private readonly ForgotPassword _useCase;

    public ForgotPasswordTests()
    {
        _users.Items.Add(User.Create("user@example.com", "Somchai", "hash", UserRole.User, _time.UtcNow.UtcDateTime));
        _useCase = new ForgotPassword(
            _users,
            _resetTokens,
            _outbox,
            new FakeSecureTokenService(),
            new ResetLinkOptions { BaseUrl = BaseUrl },
            _unitOfWork,
            _time);
    }

    [Fact]
    public async Task Forgot_ExistingUser_StoresHashedTokenAndOneEmailInOneSave()
    {
        await _useCase.ExecuteAsync("user@example.com", CancellationToken.None);

        var token = Assert.Single(_resetTokens.Items);
        var email = Assert.Single(_outbox.Items);
        Assert.Equal("hash:opaque-1", token.TokenHash);
        Assert.Equal("user@example.com", email.ToEmail);
        Assert.Equal(token.UserId, email.UserId);
        Assert.Equal(1, _unitOfWork.SaveCount);
    }

    [Fact]
    public async Task Forgot_ExistingUser_EmailBodyContainsLinkWithRawToken()
    {
        await _useCase.ExecuteAsync("user@example.com", CancellationToken.None);

        var email = Assert.Single(_outbox.Items);
        Assert.Contains($"{BaseUrl}?token=opaque-1", email.Body);
        Assert.DoesNotContain("hash:", email.Body);
    }

    [Fact]
    public async Task Forgot_UnknownEmail_WritesNothingAndDoesNotThrow()
    {
        await _useCase.ExecuteAsync("nobody@example.com", CancellationToken.None);

        Assert.Empty(_resetTokens.Items);
        Assert.Empty(_outbox.Items);
        Assert.Equal(0, _unitOfWork.SaveCount);
    }

    [Theory]
    [InlineData("", ErrorCodes.Required)]
    [InlineData(null, ErrorCodes.Required)]
    [InlineData("not-an-email", ErrorCodes.InvalidFormat)]
    public async Task Forgot_InvalidEmail_ThrowsValidationError(string? email, string fieldCode)
    {
        var exception = await Assert.ThrowsAsync<AuthException>(() => _useCase.ExecuteAsync(email, CancellationToken.None));

        Assert.Equal(ErrorCodes.ValidationError, exception.Code);
        var error = Assert.Single(exception.Errors!);
        Assert.Equal("email", error.Field);
        Assert.Equal(fieldCode, error.Code);
    }
}
