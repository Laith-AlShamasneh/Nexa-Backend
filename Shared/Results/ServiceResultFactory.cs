using Shared.Enums.System;

namespace Shared.Results;

public static class ServiceResultFactory
{
    public static ServiceResult<T> Success<T>(T data, InternalResponseCodes code, string message) =>
        new() { IsSuccess = true, Code = code, Message = message, Data = data };

    public static ServiceResult<object?> Success(InternalResponseCodes code, string message) =>
        new() { IsSuccess = true, Code = code, Message = message, Data = null };

    public static ServiceResult<T> Failure<T>(
        InternalResponseCodes code,
        string message,
        IReadOnlyList<string>? errors = null) =>
        new() { IsSuccess = false, Code = code, Message = message, Data = default, Errors = errors };
}