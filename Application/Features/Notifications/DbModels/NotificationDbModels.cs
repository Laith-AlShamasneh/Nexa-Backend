namespace Application.Features.Notifications.DbModels;

// ── Create ────────────────────────────────────────────────────────────────────

public class CreateNotificationDbModel
{
    public long     UserId       { get; set; }
    public int      TemplateId   { get; set; }
    public byte     Category     { get; set; }
    public byte     Type         { get; set; }
    public byte     Priority     { get; set; }
    public string   TitleEn      { get; set; } = null!;
    public string   TitleAr      { get; set; } = null!;
    public string   MessageEn    { get; set; } = null!;
    public string   MessageAr    { get; set; } = null!;
    public string?  PayloadJson  { get; set; }
    public DateTime? ExpiresAtUtc { get; set; }
}

// ── List ──────────────────────────────────────────────────────────────────────

public class GetNotificationsDbModel
{
    public long  UserId     { get; set; }
    public byte? Status     { get; set; }
    public byte? Category   { get; set; }
    public int   PageNumber { get; set; } = 1;
    public int   PageSize   { get; set; } = 20;
}

public class NotificationRowDbResult
{
    public long      NotificationId { get; set; }
    public byte      Category       { get; set; }
    public byte      Type           { get; set; }
    public byte      Priority       { get; set; }
    public string    TitleEn        { get; set; } = null!;
    public string    TitleAr        { get; set; } = null!;
    public string    MessageEn      { get; set; } = null!;
    public string    MessageAr      { get; set; } = null!;
    public string?   PayloadJson    { get; set; }
    public byte      Status         { get; set; }
    public DateTime  CreatedAtUtc   { get; set; }
    public DateTime? ReadAtUtc      { get; set; }
}

public class GetNotificationsDbResult
{
    public IReadOnlyList<NotificationRowDbResult> Items       { get; set; } = [];
    public int                                    TotalCount  { get; set; }
    public int                                    UnreadCount { get; set; }
}

// ── Single action (mark-read / archive / dismiss / delete) ────────────────────

public class NotificationActionDbModel
{
    public long UserId         { get; set; }
    public long NotificationId { get; set; }
}

// ── Template lookup ───────────────────────────────────────────────────────────

public class NotificationTemplateDbResult
{
    public int    TemplateId { get; set; }
    public string Code       { get; set; } = null!;
    public byte   Category   { get; set; }
    public byte   Type       { get; set; }
    public byte   Priority   { get; set; }
    public bool   IsActive   { get; set; }
}

public class NotificationTemplateTranslationDbResult
{
    public string LanguageCode    { get; set; } = null!;
    public string TitleTemplate   { get; set; } = null!;
    public string MessageTemplate { get; set; } = null!;
}

// ── Preferences ───────────────────────────────────────────────────────────────

public class NotificationPreferencesDbResult
{
    public bool SecurityEnabled  { get; set; }
    public bool FinancialEnabled { get; set; }
    public bool SystemEnabled    { get; set; }
    public bool ReportsEnabled   { get; set; }
    public bool ProfileEnabled   { get; set; }
}

public class UpsertNotificationPreferencesDbModel
{
    public long UserId           { get; set; }
    public bool SecurityEnabled  { get; set; }
    public bool FinancialEnabled { get; set; }
    public bool SystemEnabled    { get; set; }
    public bool ReportsEnabled   { get; set; }
    public bool ProfileEnabled   { get; set; }
}
