namespace Application.Common.Constants;

/// <summary>
/// Template codes that identify what kind of notification to create.
/// Each code maps to a row in NotificationTemplates and its translations.
/// </summary>
public static class NotificationCodes
{
    // ── Security ─────────────────────────────────────────────────────────────
    public const string Welcome         = "Welcome";
    public const string PasswordChanged = "PasswordChanged";
    public const string EmailChanged    = "EmailChanged";
    public const string SessionRevoked  = "SessionRevoked";
}
