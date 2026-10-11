using Auth.Domain.Users;

namespace Auth.Application.Security;

public interface IAccessTokenIssuer
{
    string Issue(User user);
}
