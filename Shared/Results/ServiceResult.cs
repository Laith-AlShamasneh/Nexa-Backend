using Shared.Enums.System;

namespace Shared.Results;

public sealed record ServiceResult<T>
{
    public bool IsSuccess { get; init; }
    public InternalResponseCodes Code { get; init; }
    public string Message { get; init; } = string.Empty;
    public T? Data { get; init; }
    public IReadOnlyList<string>? Errors { get; init; }
}