namespace Application.Common.Options;

/// <summary>
/// Resend abuse-protection settings for the Email Confirmation module. Token
/// lifetime is deliberately NOT duplicated here — it already lives in
/// <see cref="AuthenticationOptions.EmailConfirmationExpiryHours"/> (used by both
/// Tenant Onboarding's initial token and this module's resend), so there is exactly
/// one place that governs how long a confirmation token is valid. See
/// docs/EMAIL_CONFIRMATION.md "Configuration" for the full rationale.
/// </summary>
public sealed class EmailConfirmationOptions
{
    public int ResendCooldownSeconds { get; init; } = 120;
    public int MaxResendsPerHour     { get; init; } = 5;
}
