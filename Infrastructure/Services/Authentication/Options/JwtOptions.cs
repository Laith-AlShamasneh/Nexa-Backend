namespace Infrastructure.Services.Authentication.Options;

public sealed class JwtOptions
{
    public string Issuer { get; init; } = string.Empty;
    public string Audience { get; init; } = string.Empty;
    public string SecretKey { get; init; } = string.Empty;

    /// <summary>
    /// Access-token lifetime in minutes. Kept short (default 15) so a leaked
    /// access token has a small blast radius; clients use the refresh-token
    /// rotation flow for continuity. See ARCHITECTURE_DECISIONS.md §7.
    /// </summary>
    public int ExpiryMinutes { get; init; } = 15;
}
