using Application.Common.Extensions;
using Infrastructure.Extensions;
using Microsoft.AspNetCore.Diagnostics;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddOpenApi();
builder.Services.AddHealthChecks();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

var app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapHealthChecks("/health");

app.Run();

/// <summary>
/// Last-resort exception boundary: maps any exception that escapes Application-layer
/// error handling to a ProblemDetails response instead of leaking a stack trace.
/// Domain-specific exceptions (NotFoundException, ForbiddenException, ValidationAppException)
/// get their proper status codes here once the Identity phase's endpoints exist to trigger them.
/// </summary>
internal sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken ct)
    {
        logger.LogError(exception, "Unhandled exception processing {Method} {Path}",
            httpContext.Request.Method, httpContext.Request.Path);

        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;

        await httpContext.Response.WriteAsJsonAsync(new
        {
            title  = "An unexpected error occurred.",
            status = StatusCodes.Status500InternalServerError,
            traceId = httpContext.TraceIdentifier
        }, ct);

        return true;
    }
}
