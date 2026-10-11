namespace Auth.Application.Common;

public static class ErrorCodes
{
    public const string ValidationError = "VALIDATION_ERROR";
    public const string InvalidCredentials = "INVALID_CREDENTIALS";
    public const string RefreshInvalid = "REFRESH_INVALID";
    public const string RefreshReused = "REFRESH_REUSED";
    public const string Unauthorized = "UNAUTHORIZED";
    public const string CsrfInvalid = "CSRF_INVALID";
    public const string NotFound = "NOT_FOUND";
    public const string EmailExists = "EMAIL_EXISTS";
    public const string ResetTokenInvalid = "RESET_TOKEN_INVALID";
    public const string AccountLocked = "ACCOUNT_LOCKED";

    public const string Required = "REQUIRED";
    public const string InvalidFormat = "INVALID_FORMAT";
    public const string TooShort = "TOO_SHORT";
}
