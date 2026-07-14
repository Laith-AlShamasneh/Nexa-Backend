namespace Application.Interfaces.Services;

/// <summary>
/// Thin publishing façade. Any feature calls this to fire a notification —
/// implementation details (job enqueueing, template rendering, persistence)
/// are entirely hidden from the caller.
/// </summary>
public interface INotificationPublisher
{
    /// <summary>
    /// Publishes a notification asynchronously via the background job system.
    /// </summary>
    /// <param name="templateCode">A <see cref="Application.Common.Constants.NotificationCodes"/> constant.</param>
    /// <param name="userId">Target user.</param>
    /// <param name="parameters">Token-replacement values for the template, e.g. { "ChangedAt", "2025-01-01" }.</param>
    /// <param name="payload">Optional deep-link metadata serialised to JSON, e.g. new { transactionId = 123 }.</param>
    /// <param name="titleEn">Optional pre-rendered EN title (overrides template {placeholder} substitution).</param>
    /// <param name="titleAr">Optional pre-rendered AR title.</param>
    /// <param name="messageEn">Optional pre-rendered EN message.</param>
    /// <param name="messageAr">Optional pre-rendered AR message.</param>
    Task PublishAsync(
        string                      templateCode,
        long                        userId,
        Dictionary<string, string>? parameters = null,
        object?                     payload    = null,
        string?                     titleEn    = null,
        string?                     titleAr    = null,
        string?                     messageEn  = null,
        string?                     messageAr  = null,
        CancellationToken           ct         = default);
}
