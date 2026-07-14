namespace Application.Features.Notifications.DTOs;

// ── Requests ─────────────────────────────────────────────────────────────────

public record GetNotificationsRequest(
    byte? Status     = null,
    byte? Category   = null,
    int   PageNumber = 1,
    int   PageSize   = 20);

public record MarkReadRequest(long NotificationId);

public record ArchiveNotificationRequest(long NotificationId);

public record DismissNotificationRequest(long NotificationId);

public record DeleteNotificationRequest(long NotificationId);

public record UpdatePreferencesRequest(
    bool SecurityEnabled,
    bool FinancialEnabled,
    bool SystemEnabled,
    bool ReportsEnabled,
    bool ProfileEnabled);

// ── Responses ────────────────────────────────────────────────────────────────

public record NotificationResponse(
    long      NotificationId,
    string    Title,
    string    Message,
    byte      Category,
    string    CategoryName,
    byte      Type,
    string    TypeName,
    byte      Priority,
    string    PriorityName,
    byte      Status,
    string    StatusName,
    string?   PayloadJson,
    DateTime  CreatedAtUtc,
    DateTime? ReadAtUtc);

public record NotificationListResponse(
    IReadOnlyList<NotificationResponse> Items,
    int                                 TotalCount,
    int                                 PageNumber,
    int                                 PageSize,
    int                                 UnreadCount);

public record UnreadCountResponse(int Count);

public record NotificationPreferencesResponse(
    bool SecurityEnabled,
    bool FinancialEnabled,
    bool SystemEnabled,
    bool ReportsEnabled,
    bool ProfileEnabled);
