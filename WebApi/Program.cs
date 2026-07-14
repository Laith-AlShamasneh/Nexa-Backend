using System.Threading.RateLimiting;
using Application.Common.Extensions;
using Infrastructure.Extensions;
using Microsoft.AspNetCore.RateLimiting;
using Serilog;
using WebApi.Common;
using WebApi.Common.Exceptions;
using WebApi.Common.Middlewares;
using WebApi.Endpoints.Tenancy;

var builder = WebApplication.CreateBuilder(args);

// ── Serilog ───────────────────────────────────────────────────────────────────
// Ported from MyMoney: reads the "Serilog" section (Console + MSSqlServer sinks —
// see appsettings.json) and replaces the default ASP.NET Core logger provider
// entirely, so every ILogger<T> call in the app (including GlobalExceptionHandler)
// flows through it with no extra plumbing.
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .CreateLogger();

builder.Host.UseSerilog();

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

// Correlation id first so every log line for the request — including early
// pipeline and exception logs — carries it.
app.UseMiddleware<CorrelationIdMiddleware>();

app.UseExceptionHandler();
app.UseRateLimiter();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// No authentication middleware exists yet (Phase 3 — Identity and Authentication).
// MyMoney places the equivalent of this middleware after UseAuthentication/
// UseAuthorization so the logged UserId reflects the authenticated caller; move it
// there once this project adds JWT auth. Until then, IUserContext.UserId/OrganizationId
// resolve to their unauthenticated defaults.
app.UseMiddleware<RequestLoggingMiddleware>();

app.MapHealthChecks("/health");
app.MapOrganizationEndpoints();

app.Run();
