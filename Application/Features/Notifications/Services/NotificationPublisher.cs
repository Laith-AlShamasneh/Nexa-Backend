using Application.Common.Constants;
using Application.Features.Notifications.Jobs;
using Application.Interfaces.Services;
using System.Text.Json;

namespace Application.Features.Notifications.Services;

internal sealed class NotificationPublisher(IBackgroundJobService backgroundJobService) : INotificationPublisher
{
    public async Task PublishAsync(
        string                      templateCode,
        long                        userId,
        Dictionary<string, string>? parameters = null,
        object?                     payload    = null,
        string?                     titleEn    = null,
        string?                     titleAr    = null,
        string?                     messageEn  = null,
        string?                     messageAr  = null,
        CancellationToken           ct         = default)
    {
        var payloadJson = payload is not null
            ? JsonSerializer.Serialize(payload)
            : null;

        await backgroundJobService.EnqueueAsync(
            JobTypes.CreateNotification,
            new CreateNotificationPayload(templateCode, userId, parameters, payloadJson,
                                          titleEn, titleAr, messageEn, messageAr),
            priority:    2,
            maxAttempts: 3,
            ct:          ct);
    }
}
