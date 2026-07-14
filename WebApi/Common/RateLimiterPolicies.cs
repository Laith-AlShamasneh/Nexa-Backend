namespace WebApi.Common;

public static class RateLimiterPolicies
{
    /// <summary>Applied to public, pre-authentication endpoints (organization registration today).</summary>
    public const string PublicRegistration = "public-registration";
}
