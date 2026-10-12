using Auth.Application.Common;
using Auth.Application.Users;
using Auth.Domain.Users;
using Auth.UnitTests.Fakes;

namespace Auth.UnitTests.Users;

public class RegisterUserTests
{
    private readonly FakeUserRepository _users = new();
    private readonly FakeUnitOfWork _unitOfWork = new();
    private readonly RegisterUser _useCase;

    public RegisterUserTests()
    {
        _useCase = new RegisterUser(_users, new FakePasswordHasher(), _unitOfWork, new FakeTimeProvider());
    }

    [Fact]
    public async Task Register_ValidInput_StoresUserRoleWithHashedPassword()
    {
        var request = new RegisterUserRequest("user@example.com", "Passw0rd!", "Somchai");

        var result = await _useCase.ExecuteAsync(request, CancellationToken.None);

        Assert.Equal("user", result.Role);
        Assert.Equal("user@example.com", result.Email);
        var stored = Assert.Single(_users.Items);
        Assert.Equal(UserRole.User, stored.Role);
        Assert.Equal("hashed:Passw0rd!", stored.PasswordHash);
        Assert.Equal(stored.Id, result.Id);
        Assert.Equal(1, _unitOfWork.SaveCount);
    }

    [Fact]
    public async Task Register_EmailDiffersOnlyByCase_ThrowsEmailExists()
    {
        await _useCase.ExecuteAsync(new RegisterUserRequest("user@example.com", "Passw0rd!", "Somchai"), CancellationToken.None);

        var exception = await Assert.ThrowsAsync<AuthException>(() =>
            _useCase.ExecuteAsync(new RegisterUserRequest("USER@Example.com", "Passw0rd!", "Other"), CancellationToken.None));

        Assert.Equal(ErrorCodes.EmailExists, exception.Code);
        Assert.Single(_users.Items);
    }

    [Theory]
    [InlineData("", "Passw0rd!", "Somchai", "email", ErrorCodes.Required)]
    [InlineData("not-an-email", "Passw0rd!", "Somchai", "email", ErrorCodes.InvalidFormat)]
    [InlineData("user@example.com", "short", "Somchai", "password", ErrorCodes.TooShort)]
    [InlineData("user@example.com", "", "Somchai", "password", ErrorCodes.Required)]
    [InlineData("user@example.com", "Passw0rd!", "  ", "name", ErrorCodes.Required)]
    public async Task Register_InvalidField_ThrowsValidationErrorForThatField(
        string email, string password, string name, string field, string fieldCode)
    {
        var exception = await Assert.ThrowsAsync<AuthException>(() =>
            _useCase.ExecuteAsync(new RegisterUserRequest(email, password, name), CancellationToken.None));

        Assert.Equal(ErrorCodes.ValidationError, exception.Code);
        var error = Assert.Single(exception.Errors!);
        Assert.Equal(field, error.Field);
        Assert.Equal(fieldCode, error.Code);
        Assert.Empty(_users.Items);
    }

    [Fact]
    public async Task Register_AllFieldsInvalid_ReturnsThreeErrors()
    {
        var exception = await Assert.ThrowsAsync<AuthException>(() =>
            _useCase.ExecuteAsync(new RegisterUserRequest(null, null, null), CancellationToken.None));

        Assert.Equal(3, exception.Errors!.Count);
    }

    [Fact]
    public void RegisterRequest_HasNoRoleInput()
    {
        var properties = typeof(RegisterUserRequest).GetProperties().Select(p => p.Name);

        Assert.DoesNotContain("Role", properties);
    }
}
