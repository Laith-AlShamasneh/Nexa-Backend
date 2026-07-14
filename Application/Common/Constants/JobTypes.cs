namespace Application.Common.Constants;

public static class JobTypes
{
    public const string WelcomeEmail         = "WelcomeEmail";
    public const string EmailConfirmation    = "EmailConfirmation";
    public const string PasswordResetEmail   = "PasswordResetEmail";
    public const string PasswordChangedEmail = "PasswordChangedEmail";
    public const string EmailChangeRequested = "EmailChangeRequested";
    public const string EmailChanged         = "EmailChanged";
    public const string OrganizationInvitationEmail = "OrganizationInvitationEmail";

    // ── Notifications ────────────────────────────────────────────────────────
    public const string CreateNotification = "CreateNotification";
}
