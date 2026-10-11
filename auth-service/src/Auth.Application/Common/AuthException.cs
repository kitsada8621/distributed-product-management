namespace Auth.Application.Common;

public sealed class AuthException(
    string code,
    string message,
    IReadOnlyList<FieldError>? errors = null,
    object? data = null) : Exception(message)
{
    public string Code { get; } = code;
    public IReadOnlyList<FieldError>? Errors { get; } = errors;
    public object? Data { get; } = data;
}
