using Application.Features.Notifications.DTOs;
using Shared.Results;

namespace Application.Features.Notifications;

public interface INotificationService
{
    Task<ServiceResult<NotificationListResponse>> GetListAsync(GetNotificationsRequest request, CancellationToken ct = default);
    Task<ServiceResult<UnreadCountResponse>>      GetUnreadCountAsync(CancellationToken ct = default);
    Task<ServiceResult<object?>>                  MarkReadAsync(MarkReadRequest request, CancellationToken ct = default);
    Task<ServiceResult<object?>>                  MarkAllReadAsync(CancellationToken ct = default);
    Task<ServiceResult<object?>>                  ArchiveAsync(ArchiveNotificationRequest request, CancellationToken ct = default);
    Task<ServiceResult<object?>>                  DismissAsync(DismissNotificationRequest request, CancellationToken ct = default);
    Task<ServiceResult<object?>>                  DeleteAsync(DeleteNotificationRequest request, CancellationToken ct = default);
    Task<ServiceResult<NotificationPreferencesResponse>> GetPreferencesAsync(CancellationToken ct = default);
    Task<ServiceResult<object?>>                  UpdatePreferencesAsync(UpdatePreferencesRequest request, CancellationToken ct = default);
}
