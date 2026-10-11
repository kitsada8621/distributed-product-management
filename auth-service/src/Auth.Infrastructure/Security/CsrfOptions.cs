namespace Auth.Infrastructure.Security;

public sealed class CsrfOptions
{
    public string Secret { get; set; } = string.Empty;
}
