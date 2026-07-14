using Application.Features.Notifications.DbModels;

namespace Application.Interfaces.Repositories;

public interface INotificationRepository
{
    // ── Notifications ─────────────────────────────────────────────────────────
    Task<long> CreateAsync(CreateNotificationDbModel model, CancellationToken ct = default);
    Task<GetNotificationsDbResult> GetListAsync(GetNotificationsDbModel model, CancellationToken ct = default);
    Task<int> GetUnreadCountAsync(long userId, CancellationToken ct = default);
    Task<int> MarkReadAsync(NotificationActionDbModel model, CancellationToken ct = default);
    Task<int> MarkAllReadAsync(long userId, CancellationToken ct = default);
    Task<int> ArchiveAsync(NotificationActionDbModel model, CancellationToken ct = default);
    Task<int> DismissAsync(NotificationActionDbModel model, CancellationToken ct = default);
    Task<int> DeleteAsync(NotificationActionDbModel model, CancellationToken ct = default);
    Task<int> CleanupExpiredAsync(int retentionDays, CancellationToken ct = default);

    // ── Templates ─────────────────────────────────────────────────────────────
    Task<(NotificationTemplateDbResult? Template, IReadOnlyList<NotificationTemplateTranslationDbResult> Translations)>
        GetTemplateByCodeAsync(string code, CancellationToken ct = default);

    // ── Preferences ───────────────────────────────────────────────────────────
    Task<NotificationPreferencesDbResult> GetOrInitPreferencesAsync(long userId, CancellationToken ct = default);
    Task UpsertPreferencesAsync(UpsertNotificationPreferencesDbModel model, CancellationToken ct = default);
}
