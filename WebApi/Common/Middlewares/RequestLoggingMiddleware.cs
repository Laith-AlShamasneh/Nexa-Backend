using Application.Interfaces.Services;
using Serilog.Context;
using System.Text;

namespace WebApi.Common.Middlewares;

/// <summary>
/// Enriches every log line for a request with request/actor metadata — ported from
/// MyMoney, plus <see cref="LogContext.PushProperty(string, object, bool)"/> for
/// <c>OrganizationId</c> (Nexa is multi-tenant; MyMoney had no tenant concept to log).
/// Registered after authentication so <see cref="IUserContext"/> reflects the
/// authenticated caller.
/// </summary>
public sealed class RequestLoggingMiddleware(RequestDelegate next, IHostEnvironment environment)
{
    private const long MaxBodyLength = 32_768;

    public async Task InvokeAsync(HttpContext context, IUserContext userContext)
    {
        var request = context.Request;
        var path = request.Path.Value ?? string.Empty;

        // Request bodies can carry PII or credentials (passwords, tokens). Never
        // persist them to the log store in Production — only capture outside it, and
        // even then redact registration/auth endpoints. Metadata (path/method/actor/ip)
        // is always logged regardless of environment.
        var body = environment.IsDevelopment() ? await CaptureBodyAsync(request, path) : null;
        if (body is { Length: > 0 } && request.Body.CanSeek)
            request.Body.Position = 0;

        using (LogContext.PushProperty("UserId", userContext.UserId))
        using (LogContext.PushProperty("OrganizationId", userContext.OrganizationId))
        using (LogContext.PushProperty("IPAddress", userContext.IpAddress))
        using (LogContext.PushProperty("RequestPath", path))
        using (LogContext.PushProperty("RequestMethod", request.Method))
        using (LogContext.PushProperty("RequestBody", body))
        {
            await next(context);
        }
    }

    private static async Task<string?> CaptureBodyAsync(HttpRequest request, string path)
    {
        if (request.HasFormContentType && request.ContentType?.Contains("multipart/form-data") == true)
            return "[Multipart Content Skipped]";

        if (request.ContentLength is not (> 0 and < MaxBodyLength))
            return null;

        if (path.Contains("/register",        StringComparison.OrdinalIgnoreCase)
         || path.Contains("/login",           StringComparison.OrdinalIgnoreCase)
         || path.Contains("/change-password", StringComparison.OrdinalIgnoreCase)
         || path.Contains("/reset-password",  StringComparison.OrdinalIgnoreCase)
         || path.Contains("/forgot-password", StringComparison.OrdinalIgnoreCase))
            return "[REDACTED SENSITIVE DATA]";

        request.EnableBuffering();
        using var reader = new StreamReader(request.Body, Encoding.UTF8, leaveOpen: true);
        return await reader.ReadToEndAsync();
    }
}
