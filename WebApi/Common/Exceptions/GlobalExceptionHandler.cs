using Application.Interfaces.Services;
using Domain.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Shared.Constants;
using Shared.Enums.System;
using Shared.Responses;

namespace WebApi.Common.Exceptions;

/// <summary>
/// Last-resort exception boundary — ported from MyMoney's <c>GlobalExceptionHandler</c>
/// unchanged in structure. Every known Domain exception maps to its own
/// <see cref="InternalResponseCodes"/>/message key and is returned as HTTP 200 (the
/// real status lives in <see cref="ApiResponse{T}.Code"/> — see
/// <c>WebApi/Common/ApiResponseExtensions.cs</c>). Only a genuinely unrecognized
/// exception returns a real HTTP 500 — that is the one case that was not a modeled
/// business/domain outcome.
/// </summary>
public sealed class GlobalExceptionHandler(
    ILogger<GlobalExceptionHandler> logger,
    IServiceScopeFactory scopeFactory) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken ct)
    {
        logger.LogError(exception, "Unhandled exception at {Path}", httpContext.Request.Path);

        using var scope = scopeFactory.CreateScope();
        var messageProvider = scope.ServiceProvider.GetRequiredService<IMessageProvider>();

        var (internalCode, messageKey) = exception switch
        {
            ValidationAppException      => (InternalResponseCodes.BadRequest, MessageKeys.Common.BadRequest),
            UnauthorizedAccessException => (InternalResponseCodes.Unauthorized, MessageKeys.Common.Unauthorized),
            ForbiddenException          => (InternalResponseCodes.Forbidden, MessageKeys.Common.Forbidden),
            NotFoundException           => (InternalResponseCodes.NotFound, MessageKeys.Common.NotFound),
            DomainException             => (InternalResponseCodes.BadRequest, MessageKeys.Common.BadRequest),
            _                           => (InternalResponseCodes.InternalServerError, MessageKeys.Common.InternalServerError)
        };

        var httpStatusCode = exception switch
        {
            ValidationAppException      => StatusCodes.Status200OK,
            UnauthorizedAccessException => StatusCodes.Status200OK,
            ForbiddenException          => StatusCodes.Status200OK,
            NotFoundException           => StatusCodes.Status200OK,
            DomainException             => StatusCodes.Status200OK,
            _                           => StatusCodes.Status500InternalServerError
        };

        var message = await messageProvider.GetMessagesAsync(messageKey, ct);
        var response = ApiResponse<object?>.Fail((int)internalCode, message);

        httpContext.Response.StatusCode = httpStatusCode;
        await httpContext.Response.WriteAsJsonAsync(response, ct);

        return true;
    }
}
