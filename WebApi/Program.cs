using System.Threading.RateLimiting;
using Application.Common.Extensions;
using Infrastructure.Extensions;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.OpenApi;
using Serilog;
using WebApi.Common;
using WebApi.Common.Exceptions;
using WebApi.Common.Middlewares;
using WebApi.Endpoints.Dev;
using WebApi.Endpoints.Tenancy;

var builder = WebApplication.CreateBuilder(args);

// ── Uploads / large-file support ────────────────────────────────────────────────
// A single ceiling shared by Kestrel and the multipart form parser, read once here
// so both layers agree — a request Kestrel accepts but the form parser then rejects
// (or vice versa) is a confusing, hard-to-diagnose mismatch. This is the platform
// ceiling only; individual upload types (logo, profile picture, ...) enforce their
// own tighter limits in FluentValidation (see Application.Common.Upload.UploadPolicies).
//
// "Storage:MaxUploadSizeBytes" unset, zero, or negative means unlimited — Kestrel's
// MaxRequestBodySize accepts null for "no limit"; FormOptions has no such sentinel,
// so long.MaxValue stands in for it there.
var configuredUploadLimit = builder.Configuration.GetValue<long?>("Storage:MaxUploadSizeBytes");
var maxUploadSizeBytes = configuredUploadLimit is > 0 ? configuredUploadLimit : null;

builder.WebHost.ConfigureKestrel(kestrel =>
    kestrel.Limits.MaxRequestBodySize = maxUploadSizeBytes);

builder.Services.Configure<FormOptions>(form =>
{
    form.MultipartBodyLengthLimit = maxUploadSizeBytes ?? long.MaxValue;
    form.ValueLengthLimit = int.MaxValue;
});

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

// Swagger / OpenAPI — UI only mapped in Development below (see the rationale there
// before exposing it elsewhere).
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "Nexa API", Version = "v1" });

    // "Authorize" button: paste a JWT to send it as the Bearer header on calls.
    // No endpoint requires it yet (Phase 3 — Identity and Authentication), but the
    // scheme is wired up now so it's ready the moment one does.
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Paste the JWT access token (without the 'Bearer ' prefix)."
    });
});

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

// Serves everything under wwwroot/ (including wwwroot/uploads/{folder}/{file} —
// the exact path IStorageUtility.BuildFilePathWithExpiration builds for
// isInternalStorage: true) as plain static files. Placed before the rate
// limiter: static assets aren't the public-registration abuse surface the
// limiter exists for, and gating them on it would just add latency.
app.UseStaticFiles();

app.UseRateLimiter();

// API documentation — Development only. It documents the entire API surface, so put
// it behind authorization first before enabling it in another environment.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();

    // Email-template preview surface — see docs/EMAIL_TEMPLATES.md "Preview process".
    // Renders real templates with sample data; never sends mail, never reachable
    // outside Development.
    app.MapEmailTemplatePreviewEndpoints();
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
