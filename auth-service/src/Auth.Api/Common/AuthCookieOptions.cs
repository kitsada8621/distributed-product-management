namespace Auth.Api.Common;

public sealed class AuthCookieOptions
{
    public bool Secure { get; set; } = true;
    public SameSiteMode SameSite { get; set; } = SameSiteMode.Strict;
}
