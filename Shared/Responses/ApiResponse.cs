using System.Text.Json.Serialization;

namespace Shared.Responses;

public sealed record ApiResponse<T>
{
    public bool   Success { get; init; }
    public int    Code    { get; init; }
    public string Message { get; init; } = string.Empty;
    public T?     Result  { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyList<string>? Errors { get; init; }

    public static ApiResponse<T> SuccessResponse(T? result, int code, string message) =>
        new() { Success = true, Code = code, Message = message, Result = result };

    public static ApiResponse<T> Fail(
        int code,
        string message,
        IReadOnlyList<string>? errors = null) =>
        new() { Success = false, Code = code, Message = message, Result = default, Errors = errors };
}