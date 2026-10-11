using Auth.Application.Common;
using Auth.Application.Security;
using Auth.Domain.Users;

namespace Auth.Application.Users;

public sealed record RegisterUserRequest(string? Email, string? Password, string? Name);

public sealed record RegisterUserResult(Guid Id, string Email, string Role);

public sealed class RegisterUser(
    IUserRepository users,
    IPasswordHasher passwordHasher,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
{
    private const int MinPasswordLength = 8;

    public async Task<RegisterUserResult> ExecuteAsync(RegisterUserRequest request, CancellationToken cancellationToken)
    {
        var email = request.Email?.Trim() ?? string.Empty;
        var name = request.Name?.Trim() ?? string.Empty;
        var password = request.Password ?? string.Empty;
        Validate(email, password, name);

        var existing = await users.GetByEmailAsync(email, cancellationToken);
        if (existing is not null)
        {
            throw new AuthException(ErrorCodes.EmailExists, "Email is already registered");
        }

        var user = User.Create(email, name, passwordHasher.Hash(password), UserRole.User, timeProvider.GetUtcNow().UtcDateTime);
        await users.AddAsync(user, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new RegisterUserResult(user.Id, user.Email, "user");
    }

    private static void Validate(string email, string password, string name)
    {
        var errors = new List<FieldError>();

        if (email.Length == 0)
        {
            errors.Add(new FieldError("email", ErrorCodes.Required, "Email is required"));
        }
        else if (!HasEmailShape(email))
        {
            errors.Add(new FieldError("email", ErrorCodes.InvalidFormat, "Invalid email format"));
        }

        if (password.Length == 0)
        {
            errors.Add(new FieldError("password", ErrorCodes.Required, "Password is required"));
        }
        else if (password.Length < MinPasswordLength)
        {
            errors.Add(new FieldError("password", ErrorCodes.TooShort, "Password must be at least 8 characters"));
        }

        if (name.Length == 0)
        {
            errors.Add(new FieldError("name", ErrorCodes.Required, "Name is required"));
        }

        if (errors.Count > 0)
        {
            throw new AuthException(ErrorCodes.ValidationError, "Invalid input", errors);
        }
    }

    private static bool HasEmailShape(string email)
    {
        var at = email.IndexOf('@');
        return at > 0 && at < email.Length - 1 && !email.Contains(' ');
    }
}
