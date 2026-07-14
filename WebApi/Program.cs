using System.Threading.RateLimiting;
using Application.Common.Extensions;
using Infrastructure.Extensions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.RateLimiting;
using WebApi.Common;
using WebApi.Endpoints.Tenancy;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddOpenApi();
builder.Services.AddHealthChecks();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

// Public, pre-authentication endpoints get a conservative per-IP limit — there is no
// JWT/tenant context yet to key on. See docs/TENANT_ONBOARDING.md "Rate Limiting and
// Abuse Protection": this is a practical baseline, not the final anti-abuse posture
// (no CAPTCHA, no device fingerprinting yet — documented as a future enhancement).
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.AddPolicy(RateLimiterPolicies.PublicRegistration, httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));
});

var app = builder.Build();

app.UseExceptionHandler();
app.UseRateLimiter();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapHealthChecks("/health");
app.MapOrganizationEndpoints();

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
