using Shared.Enums.System;
using Shared.Responses;
using Shared.Results;

namespace WebApi.Common;

/// <summary>
/// Maps an Application-layer <see cref="ServiceResult{T}"/> to the wire-level
/// <see cref="ApiResponse{T}"/> envelope.
/// </summary>
/// <remarks>
/// By design, every handled business outcome — success, validation failure,
/// conflict, even an internal-error outcome the service caught and mapped — is
/// returned as HTTP <c>200 OK</c>. The real status the frontend acts on is
/// <see cref="ApiResponse{T}.Code"/> inside the body (200/201/400/401/404/409/500/...).
/// This keeps the transport-level HTTP status reserved for genuine transport/
/// infrastructure failures (an unhandled exception escaping to
/// <c>GlobalExceptionHandler</c>, or the rate limiter rejecting a request) — those
/// still return a real non-200 HTTP status because they are not a normal, modeled
/// business outcome.
/// </remarks>
public static class ApiResponseExtensions
{
    public static IResult ToHttpResult<T>(this ServiceResult<T> result)
    {
        var apiResponse = result.IsSuccess
            ? ApiResponse<T>.SuccessResponse(result.Data, ToCode(result.Code), result.Message)
            : ApiResponse<T>.Fail(ToCode(result.Code), result.Message, result.Errors);

        return Results.Ok(apiResponse);
    }

    private static int ToCode(InternalResponseCodes code) => code switch
    {
        InternalResponseCodes.OK                  => StatusCodes.Status200OK,
        InternalResponseCodes.Created              => StatusCodes.Status201Created,
        InternalResponseCodes.Accepted             => StatusCodes.Status202Accepted,
        InternalResponseCodes.Found                => StatusCodes.Status302Found,
        InternalResponseCodes.BadRequest           => StatusCodes.Status400BadRequest,
        InternalResponseCodes.Unauthorized         => StatusCodes.Status401Unauthorized,
        InternalResponseCodes.Forbidden            => StatusCodes.Status403Forbidden,
        InternalResponseCodes.NotFound             => StatusCodes.Status404NotFound,
        InternalResponseCodes.Conflict              => StatusCodes.Status409Conflict,
        InternalResponseCodes.InternalServerError  => StatusCodes.Status500InternalServerError,
        InternalResponseCodes.RequestTimeout       => StatusCodes.Status408RequestTimeout,
        _                                           => StatusCodes.Status500InternalServerError
    };
}
