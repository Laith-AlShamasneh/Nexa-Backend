using Domain.Common;
using Domain.Exceptions;
using Domain.Identity.Constants;

namespace Domain.Identity.Entities;

/// <summary>
/// Metadata for one issued refresh token. Only the SHA-256 hash of the raw token is
/// ever stored or exposed here — Domain never generates, hashes, or returns a raw
/// token (that's Infrastructure's <c>ITokenHasher</c>, per docs/SECURITY_BASELINE.md).
/// <see cref="Id"/> is a database IDENTITY value unknown until insert — see
/// docs/DOMAIN_MODEL.md "Dapper construction strategy" for why <see cref="AssignDatabaseId"/>
/// exists.
/// </summary>
/// <remarks>
/// <b>Rotation with reuse detection:</b> every token issued from the same original
/// login shares <see cref="TokenFamilyId"/>. <see cref="Replace"/> revokes the
/// current token and records which token replaced it; if a hash that's already
/// revoked is ever presented again, the caller (Application/Infrastructure) should
/// revoke every token sharing that <see cref="TokenFamilyId"/> — a single
/// RefreshToken instance cannot do this by itself since it requires looking up
/// siblings, so this is a cross-aggregate/Application-workflow invariant, not Domain
/// behavior.
/// </remarks>
public sealed class RefreshToken : Entity<long>
{
    public Guid UserId { get; }
    public Guid OrganizationId { get; }
    public string TokenHash { get; }
    public Guid TokenFamilyId { get; }
    public Guid? SessionId { get; }
    public DateTime ExpiresAt { get; }
    public DateTime CreatedAt { get; }
    public string? CreatedByIp { get; }
    public DateTime? RevokedAt { get; private set; }
    public Guid? RevokedBy { get; private set; }
    public string? RevokedByIp { get; private set; }
    public string? RevocationReason { get; private set; }
    public long? ReplacedByTokenId { get; private set; }

    public bool IsExpired => ExpiresAt <= DateTime.UtcNow;
    public bool IsRevoked => RevokedAt is not null;
    public bool IsActive => !IsExpired && !IsRevoked;

    private RefreshToken(long id, Guid userId, Guid organizationId, string tokenHash, Guid tokenFamilyId,
        Guid? sessionId, DateTime expiresAt, DateTime createdAt, string? createdByIp) : base(id)
    {
        UserId = userId;
        OrganizationId = organizationId;
        TokenHash = tokenHash;
        TokenFamilyId = tokenFamilyId;
        SessionId = sessionId;
        ExpiresAt = expiresAt;
        CreatedAt = createdAt;
        CreatedByIp = createdByIp;
    }

    /// <summary>Issues the first token of a new rotation family. Id is assigned later via <see cref="AssignDatabaseId"/>.</summary>
    public static RefreshToken IssueNew(Guid userId, Guid organizationId, string tokenHash, DateTime expiresAtUtc,
        string? createdByIp = null, Guid? sessionId = null) =>
        IssueInFamily(userId, organizationId, tokenHash, Guid.NewGuid(), expiresAtUtc, createdByIp, sessionId);

    /// <summary>Issues the next token in an existing rotation family (used when replacing a token — see <see cref="Replace"/>).</summary>
    public static RefreshToken IssueInFamily(Guid userId, Guid organizationId, string tokenHash, Guid tokenFamilyId,
        DateTime expiresAtUtc, string? createdByIp = null, Guid? sessionId = null)
    {
        if (userId == Guid.Empty) throw new ValidationAppException("UserId cannot be empty.");
        if (organizationId == Guid.Empty) throw new ValidationAppException("OrganizationId cannot be empty.");
        var hash = GuardTokenHash(tokenHash);
        var createdAt = DateTime.UtcNow;
        if (expiresAtUtc <= createdAt)
            throw new ValidationAppException("Refresh token expiry must be after its creation time.");

        return new RefreshToken(0, userId, organizationId, hash, tokenFamilyId, sessionId, expiresAtUtc, createdAt, createdByIp);
    }

    public static RefreshToken Reconstitute(
        long id, Guid userId, Guid organizationId, string tokenHash, Guid tokenFamilyId, Guid? sessionId,
        DateTime expiresAt, DateTime createdAt, string? createdByIp, DateTime? revokedAt, Guid? revokedBy,
        string? revokedByIp, string? revocationReason, long? replacedByTokenId)
    {
        return new RefreshToken(id, userId, organizationId, tokenHash, tokenFamilyId, sessionId, expiresAt, createdAt, createdByIp)
        {
            RevokedAt = revokedAt,
            RevokedBy = revokedBy,
            RevokedByIp = revokedByIp,
            RevocationReason = revocationReason,
            ReplacedByTokenId = replacedByTokenId
        };
    }

    /// <summary>One-time assignment of the database-generated identity value after insert.</summary>
    public void AssignDatabaseId(long id)
    {
        if (Id != 0) throw new DomainException("RefreshToken Id has already been assigned.");
        if (id <= 0) throw new ValidationAppException("Id must be positive.");
        Id = id;
    }

    public void Revoke(Guid? revokedBy, string? revokedByIp, string? reason = null)
    {
        if (IsRevoked) throw new DomainException("Refresh token is already revoked.");
        RevokedAt = DateTime.UtcNow;
        RevokedBy = revokedBy;
        RevokedByIp = revokedByIp;
        RevocationReason = GuardReason(reason);
    }

    /// <summary>Revokes this token and records which token replaced it (rotation).</summary>
    public void Replace(long replacedByTokenId, string? revokedByIp)
    {
        Revoke(revokedBy: null, revokedByIp: revokedByIp, reason: "Rotated to a new refresh token.");
        ReplacedByTokenId = replacedByTokenId;
    }

    public bool BelongsToTokenFamily(Guid tokenFamilyId) => TokenFamilyId == tokenFamilyId;

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
