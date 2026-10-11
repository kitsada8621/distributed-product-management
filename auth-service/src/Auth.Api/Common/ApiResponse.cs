namespace Auth.Api.Common;

public sealed record FieldErrorItem(string Field, string Code, string Message);

public sealed record ApiResponse(
    int Status,
    bool Success,
    string Code,
    string Message,
    object? Data,
    object? Meta,
    IReadOnlyList<FieldErrorItem>? Errors,
    string CorrelationId,
    DateTime Timestamp)
{
    public static ApiResponse Ok(HttpContext context, int status, string message, object? data)
    {
        return new ApiResponse(status, true, "SUCCESS", message, data, null, null, GetCorrelationId(context), DateTime.UtcNow);
    }

    public static ApiResponse Fail(
        HttpContext context,
        int status,
        string code,
        string message,
        IReadOnlyList<FieldErrorItem>? errors = null,
        object? data = null)
    {
        return new ApiResponse(status, false, code, message, data, null, errors, GetCorrelationId(context), DateTime.UtcNow);
    }

    public IResult ToResult()
    {
        return Results.Json(this, statusCode: Status);
    }

    private static string GetCorrelationId(HttpContext context)
    {
        return context.Items[CorrelationIdMiddleware.ItemKey] as string ?? string.Empty;
    }
}
