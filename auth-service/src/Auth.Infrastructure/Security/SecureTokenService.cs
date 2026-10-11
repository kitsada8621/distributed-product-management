using System.Buffers.Text;
using System.Security.Cryptography;
using System.Text;
using Auth.Application.Security;

namespace Auth.Infrastructure.Security;

public sealed class SecureTokenService : ISecureTokenService
{
    private const int MinSecretLength = 32;
    private const int OpaqueTokenBytes = 32;
    private const int CsrfNonceBytes = 16;

    private readonly byte[] _secret;

    public SecureTokenService(CsrfOptions options)
    {
        if (options.Secret.Length < MinSecretLength)
        {
            throw new InvalidOperationException($"Csrf:Secret must be at least {MinSecretLength} characters");
        }

        _secret = Encoding.UTF8.GetBytes(options.Secret);
    }

    public string GenerateOpaqueToken()
    {
        return Base64Url.EncodeToString(RandomNumberGenerator.GetBytes(OpaqueTokenBytes));
    }

    public string Hash(string token)
    {
        return Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
    }

    public string CreateCsrfToken(Guid familyId)
    {
        var nonce = Base64Url.EncodeToString(RandomNumberGenerator.GetBytes(CsrfNonceBytes));
        return $"{nonce}.{Sign(familyId, nonce)}";
    }

    public bool IsValidCsrfToken(string csrfToken, Guid familyId)
    {
        var parts = csrfToken.Split('.');
        if (parts.Length != 2 || parts[0].Length == 0 || parts[1].Length == 0)
        {
            return false;
        }

        var expected = Encoding.UTF8.GetBytes(Sign(familyId, parts[0]));
        var actual = Encoding.UTF8.GetBytes(parts[1]);
        return CryptographicOperations.FixedTimeEquals(expected, actual);
    }

    private string Sign(Guid familyId, string nonce)
    {
        var data = Encoding.UTF8.GetBytes($"{familyId}.{nonce}");
        return Base64Url.EncodeToString(HMACSHA256.HashData(_secret, data));
    }
}
