using Auth.Application.Security;
using Auth.Domain.Users;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Auth.Infrastructure.Security;

public sealed class JwtAccessTokenIssuer : IAccessTokenIssuer
{
    private readonly JwtOptions _options;
    private readonly TimeProvider _timeProvider;
    private readonly SigningCredentials _credentials;
    private readonly JsonWebTokenHandler _handler = new();

    public JwtAccessTokenIssuer(JwtOptions options, TimeProvider timeProvider)
    {
        _options = options;
        _timeProvider = timeProvider;
        _credentials = new SigningCredentials(new RsaSecurityKey(RsaKeyLoader.Load(options.PrivateKeyPath)), SecurityAlgorithms.RsaSha256);
    }

    public string Issue(User user)
    {
        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = _options.Issuer,
            Expires = now.AddMinutes(_options.AccessTokenMinutes),
            SigningCredentials = _credentials,
            Claims = new Dictionary<string, object>
            {
                ["sub"] = user.Id.ToString(),
                ["role"] = user.Role.ToString().ToLowerInvariant(),
                ["email"] = user.Email
            }
        };
        return _handler.CreateToken(descriptor);
    }
}
