using Application.Features.Notifications.DbModels;
using Application.Features.Notifications.DTOs;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Shared.Constants;
using Shared.Enums.Notifications;
using Shared.Enums.System;
using Shared.Results;

namespace Application.Features.Notifications.Services;

internal sealed class NotificationService(
    INotificationRepository notificationRepository,
    IUserContext            userContext,
    IMessageProvider        messageProvider) : INotificationService
{
    private bool IsArabic => userContext.Language == SystemLanguages.Arabic;

    public async Task<ServiceResult<NotificationListResponse>> GetListAsync(
        GetNotificationsRequest request,
        CancellationToken       ct = default)
    {
        var dbModel = new GetNotificationsDbModel
        {
            UserId     = userContext.UserId,
            Status     = request.Status,
            Category   = request.Category,
            PageNumber = request.PageNumber,
            PageSize   = request.PageSize
        };

        var result = await notificationRepository.GetListAsync(dbModel, ct);
        var items  = result.Items.Select(MapRow).ToList();

        var msg = await messageProvider.GetMessagesAsync(MessageKeys.Notifications.ListLoaded, ct);
        return ServiceResultFactory.Success(
            new NotificationListResponse(items, result.TotalCount, request.PageNumber, request.PageSize, result.UnreadCount),
            InternalResponseCodes.OK,
            msg);
    }

    public async Task<ServiceResult<UnreadCountResponse>> GetUnreadCountAsync(CancellationToken ct = default)
    {
        var count = await notificationRepository.GetUnreadCountAsync(userContext.UserId, ct);
        var msg   = await messageProvider.GetMessagesAsync(MessageKeys.Notifications.UnreadCountLoaded, ct);
        return ServiceResultFactory.Success(new UnreadCountResponse(count), InternalResponseCodes.OK, msg);
    }

    public async Task<ServiceResult<object?>> MarkReadAsync(MarkReadRequest request, CancellationToken ct = default)
    {
        var affected = await notificationRepository.MarkReadAsync(
            new NotificationActionDbModel { UserId = userContext.UserId, NotificationId = request.NotificationId }, ct);

        if (affected == 0)
        {
            return ServiceResultFactory.Failure<object?>(
                InternalResponseCodes.NotFound,
                await messageProvider.GetMessagesAsync(MessageKeys.Notifications.NotFound, ct));
        }

        var msg = await messageProvider.GetMessagesAsync(MessageKeys.Notifications.MarkedAsRead, ct);
        return ServiceResultFactory.Success<object?>(null, InternalResponseCodes.OK, msg);
    }

    public async Task<ServiceResult<object?>> MarkAllReadAsync(CancellationToken ct = default)
    {
        await notificationRepository.MarkAllReadAsync(userContext.UserId, ct);
        var msg = await messageProvider.GetMessagesAsync(MessageKeys.Notifications.AllMarkedAsRead, ct);
        return ServiceResultFactory.Success<object?>(null, InternalResponseCodes.OK, msg);
    }

    public async Task<ServiceResult<object?>> ArchiveAsync(ArchiveNotificationRequest request, CancellationToken ct = default)
    {
        var affected = await notificationRepository.ArchiveAsync(
            new NotificationActionDbModel { UserId = userContext.UserId, NotificationId = request.NotificationId }, ct);

        if (affected == 0)
        {
            return ServiceResultFactory.Failure<object?>(
                InternalResponseCodes.NotFound,
                await messageProvider.GetMessagesAsync(MessageKeys.Notifications.NotFound, ct));
        }

        var msg = await messageProvider.GetMessagesAsync(MessageKeys.Notifications.Archived, ct);
        return ServiceResultFactory.Success<object?>(null, InternalResponseCodes.OK, msg);
    }

    public async Task<ServiceResult<object?>> DismissAsync(DismissNotificationRequest request, CancellationToken ct = default)
    {
        var affected = await notificationRepository.DismissAsync(
            new NotificationActionDbModel { UserId = userContext.UserId, NotificationId = request.NotificationId }, ct);

        if (affected == 0)
        {
            return ServiceResultFactory.Failure<object?>(
                InternalResponseCodes.NotFound,
                await messageProvider.GetMessagesAsync(MessageKeys.Notifications.NotFound, ct));
        }

        var msg = await messageProvider.GetMessagesAsync(MessageKeys.Notifications.Dismissed, ct);
        return ServiceResultFactory.Success<object?>(null, InternalResponseCodes.OK, msg);
    }

    public async Task<ServiceResult<object?>> DeleteAsync(DeleteNotificationRequest request, CancellationToken ct = default)
    {
        var affected = await notificationRepository.DeleteAsync(
            new NotificationActionDbModel { UserId = userContext.UserId, NotificationId = request.NotificationId }, ct);

        if (affected == 0)
        {
            return ServiceResultFactory.Failure<object?>(
                InternalResponseCodes.NotFound,
                await messageProvider.GetMessagesAsync(MessageKeys.Notifications.NotFound, ct));
        }

        var msg = await messageProvider.GetMessagesAsync(MessageKeys.Notifications.Deleted, ct);
        return ServiceResultFactory.Success<object?>(null, InternalResponseCodes.OK, msg);
    }

    public async Task<ServiceResult<NotificationPreferencesResponse>> GetPreferencesAsync(CancellationToken ct = default)
    {
        var prefs = await notificationRepository.GetOrInitPreferencesAsync(userContext.UserId, ct);
        var msg   = await messageProvider.GetMessagesAsync(MessageKeys.Notifications.PreferencesLoaded, ct);
        return ServiceResultFactory.Success(MapPreferences(prefs), InternalResponseCodes.OK, msg);
    }

    public async Task<ServiceResult<object?>> UpdatePreferencesAsync(
        UpdatePreferencesRequest request,
        CancellationToken        ct = default)
    {
        await notificationRepository.UpsertPreferencesAsync(
            new UpsertNotificationPreferencesDbModel
            {
                UserId           = userContext.UserId,
                SecurityEnabled  = request.SecurityEnabled,
                FinancialEnabled = request.FinancialEnabled,
                SystemEnabled    = request.SystemEnabled,
                ReportsEnabled   = request.ReportsEnabled,
                ProfileEnabled   = request.ProfileEnabled
            }, ct);

        var msg = await messageProvider.GetMessagesAsync(MessageKeys.Notifications.PreferencesUpdated, ct);
        return ServiceResultFactory.Success<object?>(null, InternalResponseCodes.OK, msg);
    }

    // ── Mapping ────────────────────────────────────────────────────────────────

    private NotificationResponse MapRow(NotificationRowDbResult r) =>
        new(r.NotificationId,
            IsArabic ? r.TitleAr   : r.TitleEn,
            IsArabic ? r.MessageAr : r.MessageEn,
            r.Category,
            CategoryName(r.Category),
            r.Type,
            TypeName(r.Type),
            r.Priority,
            PriorityName(r.Priority),
            r.Status,
            StatusName(r.Status),
            r.PayloadJson,
            r.CreatedAtUtc,
            r.ReadAtUtc);

    private static NotificationPreferencesResponse MapPreferences(NotificationPreferencesDbResult p) =>
        new(p.SecurityEnabled, p.FinancialEnabled, p.SystemEnabled, p.ReportsEnabled, p.ProfileEnabled);

    private static string CategoryName(byte v) => (NotificationCategory)v switch
    {
        NotificationCategory.Security  => "Security",
        NotificationCategory.Financial => "Financial",
        NotificationCategory.System    => "System",
        NotificationCategory.Reports   => "Reports",
        NotificationCategory.Profile   => "Profile",
        _                              => "Unknown"
    };

    private static string TypeName(byte v) => (NotificationType)v switch
    {
        NotificationType.Information    => "Information",
        NotificationType.Success        => "Success",
        NotificationType.Warning        => "Warning",
        NotificationType.Error          => "Error",
        NotificationType.ActionRequired => "ActionRequired",
        _                               => "Unknown"
    };

    private static string PriorityName(byte v) => (NotificationPriority)v switch
    {
        NotificationPriority.Critical => "Critical",
        NotificationPriority.High     => "High",
        NotificationPriority.Normal   => "Normal",
        NotificationPriority.Low      => "Low",
        _                             => "Unknown"
    };

    private static string StatusName(byte v) => (NotificationStatus)v switch
    {
        NotificationStatus.Unread    => "Unread",
        NotificationStatus.Read      => "Read",
        NotificationStatus.Archived  => "Archived",
        NotificationStatus.Dismissed => "Dismissed",
        _                            => "Unknown"
    };
}
