using Domain.Common;
using Domain.Exceptions;
using Domain.Identity.Constants;

namespace Domain.Identity.Entities;

/// <summary>
/// A hashed, single-use password-reset token. Only the token's hash is ever stored —
/// see docs/SECURITY_BASELINE.md. No password hashing happens here; Domain manages
/// only the token's lifecycle state. Gained <see cref="OrganizationId"/> and
/// revocation fields in migration 009.
/// </summary>
public sealed class PasswordResetToken : Entity<long>
{
    public Guid UserId { get; }
    public Guid OrganizationId { get; }
    public string TokenHash { get; }
    public DateTime ExpiresAt { get; }
    public DateTime CreatedAt { get; }
    public string? RequestedByIp { get; }
    public DateTime? UsedAt { get; private set; }
    public DateTime? RevokedAt { get; private set; }
    public string? RevocationReason { get; private set; }

    public bool IsExpired => ExpiresAt <= DateTime.UtcNow;
    public bool IsUsed => UsedAt is not null;
    public bool IsRevoked => RevokedAt is not null;
    public bool IsActive => !IsExpired && !IsUsed && !IsRevoked;

    private PasswordResetToken(long id, Guid userId, Guid organizationId, string tokenHash, DateTime expiresAt,
        DateTime createdAt, string? requestedByIp) : base(id)
    {
        UserId = userId;
        OrganizationId = organizationId;
        TokenHash = tokenHash;
        ExpiresAt = expiresAt;
        CreatedAt = createdAt;
        RequestedByIp = requestedByIp;
    }

    public static PasswordResetToken IssueNew(Guid userId, Guid organizationId, string tokenHash,
        DateTime expiresAtUtc, string? requestedByIp = null)
    {
        if (userId == Guid.Empty) throw new ValidationAppException("UserId cannot be empty.");
        if (organizationId == Guid.Empty) throw new ValidationAppException("OrganizationId cannot be empty.");
        var hash = GuardTokenHash(tokenHash);
        var createdAt = DateTime.UtcNow;
        if (expiresAtUtc <= createdAt)
            throw new ValidationAppException("Token expiry must be after its creation time.");

        return new PasswordResetToken(0, userId, organizationId, hash, expiresAtUtc, createdAt, requestedByIp);
    }

    public static PasswordResetToken Reconstitute(
        long id, Guid userId, Guid organizationId, string tokenHash, DateTime expiresAt, DateTime createdAt,
        string? requestedByIp, DateTime? usedAt, DateTime? revokedAt, string? revocationReason)
    {
        return new PasswordResetToken(id, userId, organizationId, tokenHash, expiresAt, createdAt, requestedByIp)
        {
            UsedAt = usedAt,
            RevokedAt = revokedAt,
            RevocationReason = revocationReason
        };
    }

    public void AssignDatabaseId(long id)
    {
        if (Id != 0) throw new DomainException("PasswordResetToken Id has already been assigned.");
        if (id <= 0) throw new ValidationAppException("Id must be positive.");
        Id = id;
    }

    public void MarkUsed()
    {
        if (IsUsed) throw new DomainException("Token has already been used.");
        if (IsRevoked) throw new DomainException("Token has been revoked and cannot be used.");
        UsedAt = DateTime.UtcNow;
    }

    public void Revoke(string? reason = null)
    {
        if (IsUsed) throw new DomainException("Cannot revoke a token that has already been used.");
        if (IsRevoked) throw new DomainException("Token is already revoked.");
        RevokedAt = DateTime.UtcNow;
        RevocationReason = GuardReason(reason);
    }

    private static string GuardTokenHash(string tokenHash)
    {
        if (string.IsNullOrWhiteSpace(tokenHash) || tokenHash.Length != IdentityLengths.TokenHashLength)
            throw new ValidationAppException($"Token hash must be a {IdentityLengths.TokenHashLength}-character SHA-256 hex digest.");
        return tokenHash;
    }

    private static string? GuardReason(string? reason)
    {
        if (reason is { Length: > 0 } && reason.Length > IdentityLengths.RevocationReason.MaxLength)
            throw new ValidationAppException($"Revocation reason cannot exceed {IdentityLengths.RevocationReason.MaxLength} characters.");
        return reason;
    }
}
