namespace Catalog.Application.Common;

public sealed class CatalogException(
    string code,
    string message,
    IReadOnlyList<FieldError>? errors = null,
    object? data = null) : Exception(message)
{
    public string Code { get; } = code;
    public IReadOnlyList<FieldError>? Errors { get; } = errors;
    public new object? Data { get; } = data;
}
