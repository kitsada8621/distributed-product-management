using Auth.Application.Security;
using Auth.Domain.Users;

namespace Auth.UnitTests.Fakes;

public sealed class FakeAccessTokenIssuer : IAccessTokenIssuer
{
    public string Issue(User user) => "access:" + user.Id;
}
