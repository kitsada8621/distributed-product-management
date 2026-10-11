namespace Catalog.Infrastructure.Security;

public sealed class JwtOptions
{
    public string Issuer { get; set; } = string.Empty;

    public string PublicKeyPath { get; set; } = string.Empty;
}
