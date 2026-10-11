using Auth.Application.Common;

namespace Auth.Application.Users;

public sealed record CurrentUserResult(Guid Id, string Email, string Name, string Role);

public sealed class GetCurrentUser(IUserRepository users)
{
    public async Task<CurrentUserResult> ExecuteAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await users.GetByIdAsync(userId, cancellationToken)
            ?? throw new AuthException(ErrorCodes.Unauthorized, "Authentication required");

        return new CurrentUserResult(user.Id, user.Email, user.Name, user.Role.ToString().ToLowerInvariant());
    }
}
