namespace Domain.Identity.Constants;

/// <summary>
/// Conventional values for <see cref="Entities.SignInLog.EventType"/>. The column is
/// an open NVARCHAR(50) with no CHECK constraint (a tenant/auth-method mix that isn't
/// fully known yet), so this is a suggested vocabulary, not an exhaustive enum — see
/// docs/DOMAIN_MODEL.md for why this is a constants class rather than an enum.
/// </summary>
public static class SignInEventTypes
{
    /// <summary>The database column default (<c>DF_SignInLogs_EventType</c>).</summary>
    public const string PasswordSignIn = "PasswordSignIn";
}
