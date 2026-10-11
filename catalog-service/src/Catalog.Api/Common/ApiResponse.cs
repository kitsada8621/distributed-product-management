namespace Catalog.Api.Common;

public sealed record FieldErrorItem(string Field, string Code, string Message);

public sealed record ApiResponse(
    int Status,
    bool Success,
    string Code,
    string Message,
    object? Data,
    object? Meta,
    IReadOnlyList<FieldErrorItem>? Errors)
{
    public static ApiResponse Ok(int status, string message, object? data, object? meta = null)
    {
        return new ApiResponse(status, true, "SUCCESS", message, data, meta, null);
    }

    public static ApiResponse Fail(
        int status,
        string code,
        string message,
        IReadOnlyList<FieldErrorItem>? errors = null,
        object? data = null)
    {
        return new ApiResponse(status, false, code, message, data, null, errors);
    }

    public IResult ToResult()
    {
        return Results.Json(this, statusCode: Status);
    }
}
