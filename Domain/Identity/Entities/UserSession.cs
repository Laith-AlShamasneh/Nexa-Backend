using Domain.Common;
using Domain.Exceptions;
using Domain.Identity.Constants;

namespace Domain.Identity.Entities;

/// <summary>
/// One authenticated device/session, introduced by migration 009 to back
/// "log out everywhere" and "see your active devices." <see cref="RefreshToken.SessionId"/>
/// links a refresh token to the session that issued it. No HTTP/ClaimsPrincipal
/// concepts here — Infrastructure extracts device/IP/user-agent values from the
/// request and passes them in as plain strings.
/// </summary>
public sealed class UserSession : Entity<Guid>, ITenantOwned
{
    public Guid OrganizationId { get; }
    public Guid UserId { get; }
    public string? DeviceId { get; }
    public string? DeviceName { get; }
    public string? UserAgent { get; }
    public string? IpAddress { get; }
    public DateTime CreatedAt { get; }
    public DateTime LastSeenAt { get; private set; }
    public DateTime? ExpiresAt { get; }
    public DateTime? RevokedAt { get; private set; }
    public Guid? RevokedBy { get; private set; }
    public string? RevocationReason { get; private set; }

    public bool IsExpired => ExpiresAt is { } expiresAt && expiresAt <= DateTime.UtcNow;
    public bool IsRevoked => RevokedAt is not null;
    public bool IsActive => !IsExpired && !IsRevoked;

    private UserSession(Guid id, Guid organizationId, Guid userId, string? deviceId, string? deviceName,
        string? userAgent, string? ipAddress, DateTime createdAt, DateTime? expiresAt) : base(id)
    {
        OrganizationId = organizationId;
        UserId = userId;
        DeviceId = deviceId;
        DeviceName = deviceName;
        UserAgent = userAgent;
        IpAddress = ipAddress;
        CreatedAt = createdAt;
        LastSeenAt = createdAt;
        ExpiresAt = expiresAt;
    }

    public static UserSession Open(Guid organizationId, Guid userId, string? deviceId = null,
        string? deviceName = null, string? userAgent = null, string? ipAddress = null, DateTime? expiresAtUtc = null)
    {
        if (organizationId == Guid.Empty) throw new ValidationAppException("OrganizationId cannot be empty.");
        if (userId == Guid.Empty) throw new ValidationAppException("UserId cannot be empty.");

        var createdAt = DateTime.UtcNow;
        if (expiresAtUtc is { } expiresAt && expiresAt <= createdAt)
            throw new ValidationAppException("Session expiry must be after its creation time.");

        return new UserSession(Guid.CreateVersion7(), organizationId, userId, deviceId, deviceName, userAgent,
            ipAddress, createdAt, expiresAtUtc);
    }

    public static UserSession Reconstitute(
        Guid id, Guid organizationId, Guid userId, string? deviceId, string? deviceName, string? userAgent,
        string? ipAddress, DateTime createdAt, DateTime lastSeenAt, DateTime? expiresAt, DateTime? revokedAt,
        Guid? revokedBy, string? revocationReason)
    {
        return new UserSession(id, organizationId, userId, deviceId, deviceName, userAgent, ipAddress, createdAt, expiresAt)
        {
            LastSeenAt = lastSeenAt,
            RevokedAt = revokedAt,
            RevokedBy = revokedBy,
            RevocationReason = revocationReason
        };
    }

    /// <summary>Records recent activity on this session.</summary>
    public void Touch()
    {
        if (IsRevoked) throw new DomainException("Cannot touch a revoked session.");
        LastSeenAt = DateTime.UtcNow;
    }

    public void Revoke(Guid? revokedBy, string? reason = null)
    {
        if (IsRevoked) throw new DomainException("Session is already revoked.");
        RevokedAt = DateTime.UtcNow;
        RevokedBy = revokedBy;
        RevocationReason = GuardReason(reason);
    }

    private static string? GuardReason(string? reason)
    {
        if (reason is { Length: > 0 } && reason.Length > IdentityLengths.RevocationReason.MaxLength)
            throw new ValidationAppException($"Revocation reason cannot exceed {IdentityLengths.RevocationReason.MaxLength} characters.");
        return reason;
    }
}
