namespace Application.Common.Options;

public sealed class AuthenticationOptions
{
    public int    MaxFailedLoginAttempts       { get; init; } = 5;
    public int    LockoutDurationMinutes       { get; init; } = 30;
    public int    EmailConfirmationExpiryHours { get; init; } = 24;
    public string ConfirmEmailBaseUrl          { get; init; } = string.Empty;
    public int    PasswordResetExpiryMinutes   { get; init; } = 15;
    public string ResetPasswordBaseUrl         { get; init; } = string.Empty;
    public string AcceptInvitationBaseUrl      { get; init; } = string.Empty;

    // H8: when true, access tokens carry a per-user security stamp that is validated
    // on each request, and the stamp is bumped on password change (revoking tokens).
    // Keep false until the audit-h8-security-stamp.sql migration is applied.
    public bool   ValidateAccessTokenStamp     { get; init; }
}
