namespace Auth.Infrastructure.Security;

public sealed class JwtOptions
{
    public string Issuer { get; set; } = string.Empty;

    public string PrivateKeyPath { get; set; } = string.Empty;

    public int AccessTokenMinutes { get; set; } = 15;
}
