using Auth.Application.Common;
using Auth.Application.Users;
using Auth.Domain.Users;
using Auth.UnitTests.Fakes;

namespace Auth.UnitTests.Users;

public class GetCurrentUserTests
{
    private static readonly DateTime Now = new(2026, 10, 10, 8, 30, 0, DateTimeKind.Utc);

    private readonly FakeUserRepository _users = new();
    private readonly GetCurrentUser _useCase;

    public GetCurrentUserTests()
    {
        _useCase = new GetCurrentUser(_users);
    }

    [Fact]
    public async Task GetCurrentUser_ReturnsAllFourFields()
    {
        var user = AddUser(UserRole.User);

        var result = await _useCase.ExecuteAsync(user.Id, CancellationToken.None);

        Assert.Equal(user.Id, result.Id);
        Assert.Equal("user@example.com", result.Email);
        Assert.Equal("Somchai", result.Name);
        Assert.Equal("user", result.Role);
    }

    [Fact]
    public async Task GetCurrentUser_Admin_MapsRoleToLowercaseAdmin()
    {
        var admin = AddUser(UserRole.Admin);

        var result = await _useCase.ExecuteAsync(admin.Id, CancellationToken.None);

        Assert.Equal("admin", result.Role);
    }

    [Fact]
    public async Task GetCurrentUser_MissingUser_ThrowsUnauthorized()
    {
        var exception = await Assert.ThrowsAsync<AuthException>(() =>
            _useCase.ExecuteAsync(Guid.NewGuid(), CancellationToken.None));

        Assert.Equal(ErrorCodes.Unauthorized, exception.Code);
    }

    private User AddUser(UserRole role)
    {
        var user = User.Create("user@example.com", "Somchai", "hash", role, Now);
        _users.Items.Add(user);
        return user;
    }
}
