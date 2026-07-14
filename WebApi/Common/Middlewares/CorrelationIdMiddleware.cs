using Serilog.Context;

namespace WebApi.Common.Middlewares;

/// <summary>
/// Assigns a correlation id to every request — ported from MyMoney unchanged.
/// Honours an inbound <c>X-Correlation-Id</c> header (so a call chain shares one id)
/// or generates a new GUID. The id is echoed on the response and pushed to
/// Serilog's <see cref="LogContext"/> so every log line for the request carries it.
/// Registered first in the pipeline so even early-pipeline/exception logs are
/// correlated.
/// </summary>
public sealed class CorrelationIdMiddleware(RequestDelegate next)
{
    public const string HeaderName = "X-Correlation-Id";

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId =
            context.Request.Headers.TryGetValue(HeaderName, out var inbound)
            && !string.IsNullOrWhiteSpace(inbound)
                ? inbound.ToString()
                : Guid.NewGuid().ToString();

        // Exposed via HttpContext.Items (not just LogContext) so non-Serilog code —
        // IUserContext.TraceId, and anything that stamps an audit row with a
        // correlation id — agrees with what Serilog logged for this request.
        context.Items["CorrelationId"] = correlationId;

        context.Response.OnStarting(() =>
        {
            context.Response.Headers[HeaderName] = correlationId;
            return Task.CompletedTask;
        });

        using (LogContext.PushProperty("CorrelationId", correlationId))
        {
            await next(context);
        }
    }
}
