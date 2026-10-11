using Auth.Api.Common;
using Auth.Application.Common;
using Auth.Application.Tokens;
using Auth.Application.Users;

namespace Auth.Api.Endpoints;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth");

        group.MapPost("/register", RegisterAsync);
        group.MapPost("/login", LoginAsync);
        group.MapPost("/refresh", RefreshAsync).AddEndpointFilter<OriginCheckFilter>();
        group.MapPost("/logout", LogoutAsync).RequireAuthorization().AddEndpointFilter<OriginCheckFilter>();
        group.MapPost("/forgot-password", ForgotPasswordAsync);
        group.MapPost("/reset-password", ResetPasswordAsync);
        group.MapGet("/me", GetCurrentUserAsync).RequireAuthorization();
    }

    private static async Task<IResult> RegisterAsync(
        RegisterUserRequest request, RegisterUser registerUser, HttpContext context, CancellationToken cancellationToken)
    {
        var result = await registerUser.ExecuteAsync(request, cancellationToken);
        return ApiResponse.Ok(context, StatusCodes.Status201Created, "Registered", result).ToResult();
    }

    private static async Task<IResult> LoginAsync(
        LoginRequest request, LoginUser loginUser, AuthCookieOptions cookieOptions, HttpContext context, CancellationToken cancellationToken)
    {
        var result = await loginUser.ExecuteAsync(request, cancellationToken);
        AuthCookies.Write(context.Response, cookieOptions, result.RefreshToken, result.CsrfToken);
        return ApiResponse.Ok(context, StatusCodes.Status200OK, "Login successful", new { accessToken = result.AccessToken }).ToResult();
    }

    private static async Task<IResult> RefreshAsync(
        RefreshSession refreshSession, AuthCookieOptions cookieOptions, HttpContext context, CancellationToken cancellationToken)
    {
        var request = new RefreshRequest(
            context.Request.Cookies[AuthCookies.RefreshTokenName],
            context.Request.Headers[AuthCookies.CsrfHeaderName].FirstOrDefault(),
            context.Request.Cookies[AuthCookies.CsrfTokenName]);

        var result = await refreshSession.ExecuteAsync(request, cancellationToken);
        AuthCookies.Write(context.Response, cookieOptions, result.RefreshToken, result.CsrfToken);
        return ApiResponse.Ok(context, StatusCodes.Status200OK, "Token refreshed", new { accessToken = result.AccessToken }).ToResult();
    }

    private static async Task<IResult> LogoutAsync(
        LogoutSession logoutSession, AuthCookieOptions cookieOptions, HttpContext context, CancellationToken cancellationToken)
    {
        var subject = context.User.FindFirst("sub")?.Value;
        if (!Guid.TryParse(subject, out var userId))
        {
            throw new AuthException(ErrorCodes.Unauthorized, "Authentication required");
        }

        var request = new LogoutRequest(
            userId,
            context.Request.Cookies[AuthCookies.RefreshTokenName],
            context.Request.Headers[AuthCookies.CsrfHeaderName].FirstOrDefault(),
            context.Request.Cookies[AuthCookies.CsrfTokenName]);

        await logoutSession.ExecuteAsync(request, cancellationToken);
        AuthCookies.Clear(context.Response, cookieOptions);
        return ApiResponse.Ok(context, StatusCodes.Status200OK, "Logged out", null).ToResult();
    }

    private static async Task<IResult> ForgotPasswordAsync(
        ForgotPasswordRequest request, ForgotPassword forgotPassword, HttpContext context, CancellationToken cancellationToken)
    {
        await forgotPassword.ExecuteAsync(request.Email, cancellationToken);
        return ApiResponse.Ok(context, StatusCodes.Status202Accepted, "If the account exists, an email has been sent", null).ToResult();
    }

    private static async Task<IResult> ResetPasswordAsync(
        ResetPasswordRequest request, ResetPassword resetPassword, HttpContext context, CancellationToken cancellationToken)
    {
        await resetPassword.ExecuteAsync(request, cancellationToken);
        return ApiResponse.Ok(context, StatusCodes.Status200OK, "Password has been reset", null).ToResult();
    }

    private static async Task<IResult> GetCurrentUserAsync(
        GetCurrentUser getCurrentUser, HttpContext context, CancellationToken cancellationToken)
    {
        var subject = context.User.FindFirst("sub")?.Value;
        if (!Guid.TryParse(subject, out var userId))
        {
            throw new AuthException(ErrorCodes.Unauthorized, "Authentication required");
        }

        var result = await getCurrentUser.ExecuteAsync(userId, cancellationToken);
        return ApiResponse.Ok(context, StatusCodes.Status200OK, "OK", result).ToResult();
    }
}
