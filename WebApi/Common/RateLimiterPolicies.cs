namespace WebApi.Common;

public static class RateLimiterPolicies
{
    /// <summary>Applied to public, pre-authentication endpoints (organization registration today).</summary>
    public const string PublicRegistration = "public-registration";

    /// <summary>
    /// Applied to the confirm-email and resend-confirmation endpoints. This is the
    /// transport-level, per-IP abuse control; the resend endpoint additionally
    /// enforces a per-user cooldown and hourly cap at the database level (see
    /// docs/EMAIL_CONFIRMATION.md "Rate limiting and cooldown") — the two are
    /// independent layers, not a substitute for one another.
    /// </summary>
    public const string PublicEmailConfirmation = "public-email-confirmation";
}
