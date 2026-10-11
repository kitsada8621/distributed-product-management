namespace Auth.Api.Common;

public static class AuthCookies
{
    public const string RefreshTokenName = "refreshToken";
    public const string CsrfTokenName = "csrfToken";
    public const string CsrfHeaderName = "X-CSRF-Token";

    private const string RefreshTokenPath = "/api/auth";
    private const string CsrfTokenPath = "/";
    private static readonly TimeSpan Lifetime = TimeSpan.FromDays(7);

    public static void Write(HttpResponse response, AuthCookieOptions options, string refreshToken, string csrfToken)
    {
        var refreshOptions = Options(httpOnly: true, RefreshTokenPath, options);
        refreshOptions.MaxAge = Lifetime;
        response.Cookies.Append(RefreshTokenName, refreshToken, refreshOptions);

        var csrfOptions = Options(httpOnly: false, CsrfTokenPath, options);
        csrfOptions.MaxAge = Lifetime;
        response.Cookies.Append(CsrfTokenName, csrfToken, csrfOptions);
    }

    public static void Clear(HttpResponse response, AuthCookieOptions options)
    {
        response.Cookies.Delete(RefreshTokenName, Options(httpOnly: true, RefreshTokenPath, options));
        response.Cookies.Delete(CsrfTokenName, Options(httpOnly: false, CsrfTokenPath, options));
    }

    private static CookieOptions Options(bool httpOnly, string path, AuthCookieOptions options)
    {
        return new CookieOptions
        {
            HttpOnly = httpOnly,
            Secure = options.Secure,
            SameSite = options.SameSite,
            Path = path
        };
    }
}
