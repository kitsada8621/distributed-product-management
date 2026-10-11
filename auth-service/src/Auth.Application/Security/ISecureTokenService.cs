namespace Auth.Application.Security;

public interface ISecureTokenService
{
    string GenerateOpaqueToken();

    string Hash(string token);

    string CreateCsrfToken(Guid familyId);

    bool IsValidCsrfToken(string csrfToken, Guid familyId);
}
