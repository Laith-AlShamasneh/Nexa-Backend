namespace Application.Features.Authentication.DbModels;

/// <summary>
/// Used only by AuthService.RegisterAsync — a separate, currently-unwired
/// registration flow out of scope for the Email Confirmation module (see
/// docs/EMAIL_CONFIRMATION.md). Kept only so that method still compiles; it calls
/// identity.usp_Authentication_SaveConfirmationToken, which does not exist in any
/// migration (same pre-existing gap documented in docs/SECURITY_BASELINE.md before
/// this module's work began).
/// </summary>
public sealed class SaveConfirmationTokenDbInput
{
    public long     UserId       { get; init; }
    public string   TokenHash    { get; init; } = null!;
    public DateTime ExpiresAtUtc { get; init; }
    public string?  CreatedByIp  { get; init; }
}
