using Auth.Application.Security;

namespace Auth.UnitTests.Fakes;

public sealed class FakeSecureTokenService : ISecureTokenService
{
    private int _counter;

    public string GenerateOpaqueToken() => "opaque-" + ++_counter;

    public string Hash(string token) => "hash:" + token;

    public string CreateCsrfToken(Guid familyId) => "csrf:" + familyId;

    public bool IsValidCsrfToken(string csrfToken, Guid familyId) => csrfToken == CreateCsrfToken(familyId);
}
