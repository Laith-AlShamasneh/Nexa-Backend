using Application.Features.Notifications.DbModels;
using Application.Interfaces.Database;
using Application.Interfaces.Repositories;
using Dapper;
using System.Data;

namespace Infrastructure.Services.Notifications;

internal sealed class NotificationRepository(IDbExecutor db) : INotificationRepository
{
    public async Task<long> CreateAsync(CreateNotificationDbModel model, CancellationToken ct = default)
    {
        var p = new DynamicParameters();
        p.Add("@UserId",      model.UserId,      DbType.Int64);
        p.Add("@TemplateId",  model.TemplateId,  DbType.Int32);
        p.Add("@Category",    model.Category,    DbType.Byte);
        p.Add("@Type",        model.Type,        DbType.Byte);
        p.Add("@Priority",    model.Priority,    DbType.Byte);
        p.Add("@TitleEn",     model.TitleEn,     DbType.String);
        p.Add("@TitleAr",     model.TitleAr,     DbType.String);
        p.Add("@MessageEn",   model.MessageEn,   DbType.String);
        p.Add("@MessageAr",   model.MessageAr,   DbType.String);
        p.Add("@PayloadJson", model.PayloadJson, DbType.String);
        p.Add("@ExpiresAtUtc",model.ExpiresAtUtc,DbType.DateTime2);
        p.Add("@NewId", dbType: DbType.Int64, direction: ParameterDirection.Output);

        await db.ExecuteAsync("MyMoney.usp_Notification_Create", p, ct);
        return p.Get<long>("@NewId");
    }

    public Task<GetNotificationsDbResult> GetListAsync(GetNotificationsDbModel model, CancellationToken ct = default)
    {
        var p = new DynamicParameters();
        p.Add("@UserId",     model.UserId,     DbType.Int64);
        p.Add("@Status",     model.Status,     DbType.Byte);
        p.Add("@Category",   model.Category,   DbType.Byte);
        p.Add("@PageNumber", model.PageNumber, DbType.Int32);
        p.Add("@PageSize",   model.PageSize,   DbType.Int32);

        return db.QueryMultipleAsync(
            "MyMoney.usp_Notification_GetList",
            async multi =>
            {
                var items  = (await multi.ReadAsync<NotificationRowDbResult>()).AsList();
                var counts = await multi.ReadFirstOrDefaultAsync<NotificationListCountsRow>()
                             ?? new NotificationListCountsRow(0, 0);

                return new GetNotificationsDbResult
                {
                    Items       = items,
                    TotalCount  = counts.TotalCount,
                    UnreadCount = counts.UnreadCount
                };
            },
            p, ct);
    }

    public async Task<int> GetUnreadCountAsync(long userId, CancellationToken ct = default)
    {
        var p = new DynamicParameters();
        p.Add("@UserId", userId, DbType.Int64);
        return await db.ExecuteScalarAsync<int>("MyMoney.usp_Notification_GetUnreadCount", p, ct);
    }

    public async Task<int> MarkReadAsync(NotificationActionDbModel model, CancellationToken ct = default)
    {
        var p = new DynamicParameters();
        p.Add("@UserId",         model.UserId,         DbType.Int64);
        p.Add("@NotificationId", model.NotificationId, DbType.Int64);
        p.Add("@RowsAffected",   dbType: DbType.Int32, direction: ParameterDirection.Output);
        await db.ExecuteAsync("MyMoney.usp_Notification_MarkRead", p, ct);
        return p.Get<int>("@RowsAffected");
    }

    public async Task<int> MarkAllReadAsync(long userId, CancellationToken ct = default)
    {
        var p = new DynamicParameters();
        p.Add("@UserId",       userId,  DbType.Int64);
        p.Add("@RowsAffected", dbType: DbType.Int32, direction: ParameterDirection.Output);
        await db.ExecuteAsync("MyMoney.usp_Notification_MarkAllRead", p, ct);
        return p.Get<int>("@RowsAffected");
    }

    public async Task<int> ArchiveAsync(NotificationActionDbModel model, CancellationToken ct = default)
    {
        var p = new DynamicParameters();
        p.Add("@UserId",         model.UserId,         DbType.Int64);
        p.Add("@NotificationId", model.NotificationId, DbType.Int64);
        p.Add("@RowsAffected",   dbType: DbType.Int32, direction: ParameterDirection.Output);
        await db.ExecuteAsync("MyMoney.usp_Notification_Archive", p, ct);
        return p.Get<int>("@RowsAffected");
    }

    public async Task<int> DismissAsync(NotificationActionDbModel model, CancellationToken ct = default)
    {
        var p = new DynamicParameters();
        p.Add("@UserId",         model.UserId,         DbType.Int64);
        p.Add("@NotificationId", model.NotificationId, DbType.Int64);
        p.Add("@RowsAffected",   dbType: DbType.Int32, direction: ParameterDirection.Output);
        await db.ExecuteAsync("MyMoney.usp_Notification_Dismiss", p, ct);
        return p.Get<int>("@RowsAffected");
    }

    public async Task<int> DeleteAsync(NotificationActionDbModel model, CancellationToken ct = default)
    {
        var p = new DynamicParameters();
        p.Add("@UserId",         model.UserId,         DbType.Int64);
        p.Add("@NotificationId", model.NotificationId, DbType.Int64);
        p.Add("@RowsAffected",   dbType: DbType.Int32, direction: ParameterDirection.Output);
        await db.ExecuteAsync("MyMoney.usp_Notification_Delete", p, ct);
        return p.Get<int>("@RowsAffected");
    }

    public async Task<int> CleanupExpiredAsync(int retentionDays, CancellationToken ct = default)
    {
        var p = new DynamicParameters();
        p.Add("@RetentionDays", retentionDays, DbType.Int32);
        p.Add("@RowsDeleted",   dbType: DbType.Int32, direction: ParameterDirection.Output);
        await db.ExecuteAsync("MyMoney.usp_Notification_CleanupExpired", p, ct);
        return p.Get<int>("@RowsDeleted");
    }

    public Task<(NotificationTemplateDbResult? Template, IReadOnlyList<NotificationTemplateTranslationDbResult> Translations)>
        GetTemplateByCodeAsync(string code, CancellationToken ct = default)
    {
        var p = new DynamicParameters();
        p.Add("@Code", code, DbType.String);

        return db.QueryMultipleAsync(
            "MyMoney.usp_NotificationTemplate_GetByCode",
            async multi =>
            {
                var template     = await multi.ReadFirstOrDefaultAsync<NotificationTemplateDbResult>();
                var translations = (await multi.ReadAsync<NotificationTemplateTranslationDbResult>()).AsList();
                return (template, (IReadOnlyList<NotificationTemplateTranslationDbResult>)translations);
            },
            p, ct);
    }

    public async Task<NotificationPreferencesDbResult> GetOrInitPreferencesAsync(long userId, CancellationToken ct = default)
    {
        var p = new DynamicParameters();
        p.Add("@UserId", userId, DbType.Int64);
        return await db.QuerySingleAsync<NotificationPreferencesDbResult>(
            "MyMoney.usp_NotificationPreferences_GetOrInit", p, ct)
            ?? new NotificationPreferencesDbResult
            {
                SecurityEnabled  = true,
                FinancialEnabled = true,
                SystemEnabled    = true,
                ReportsEnabled   = true,
                ProfileEnabled   = true
            };
    }

    public Task UpsertPreferencesAsync(UpsertNotificationPreferencesDbModel model, CancellationToken ct = default)
    {
        var p = new DynamicParameters();
        p.Add("@UserId",           model.UserId,           DbType.Int64);
        p.Add("@SecurityEnabled",  model.SecurityEnabled,  DbType.Boolean);
        p.Add("@FinancialEnabled", model.FinancialEnabled, DbType.Boolean);
        p.Add("@SystemEnabled",    model.SystemEnabled,    DbType.Boolean);
        p.Add("@ReportsEnabled",   model.ReportsEnabled,   DbType.Boolean);
        p.Add("@ProfileEnabled",   model.ProfileEnabled,   DbType.Boolean);
        return db.ExecuteAsync("MyMoney.usp_NotificationPreferences_Upsert", p, ct);
    }

    private sealed record NotificationListCountsRow(int TotalCount, int UnreadCount);
}
