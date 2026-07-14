using Domain.Common;
using Domain.Exceptions;
using Domain.Identity.Constants;

namespace Domain.Identity.Entities;

/// <summary>
/// An email invitation to join an organization with a specific (tenant-local) role —
/// introduced by migration 009 as the "add a teammate" counterpart to owner
/// self-registration. Only the invitation token's hash is stored. Accepting an
/// invitation (creating the invitee's Person/User/UserRole rows) is an Application
/// workflow — this entity only tracks the invitation's own lifecycle.
/// </summary>
public sealed class UserInvitation : Entity<long>, ITenantOwned
{
    public Guid OrganizationId { get; }
    public string Email { get; }
    public string? NormalizedEmail { get; private set; }
    public Guid RoleId { get; }
    public string TokenHash { get; }
    public Guid InvitedBy { get; }
    public DateTime ExpiresAt { get; }
    public DateTime CreatedAt { get; }
    public DateTime? AcceptedAt { get; private set; }
    public DateTime? RevokedAt { get; private set; }
    public string? RevocationReason { get; private set; }

    public bool IsExpired => ExpiresAt <= DateTime.UtcNow;
    public bool IsAccepted => AcceptedAt is not null;
    public bool IsRevoked => RevokedAt is not null;
    public bool IsActive => !IsExpired && !IsAccepted && !IsRevoked;

    private UserInvitation(long id, Guid organizationId, string email, Guid roleId, string tokenHash,
        Guid invitedBy, DateTime expiresAt, DateTime createdAt) : base(id)
    {
        OrganizationId = organizationId;
        Email = email;
        RoleId = roleId;
        TokenHash = tokenHash;
        InvitedBy = invitedBy;
        ExpiresAt = expiresAt;
        CreatedAt = createdAt;
    }

    public static UserInvitation Issue(Guid organizationId, string email, Guid roleId, string tokenHash,
        Guid invitedBy, DateTime expiresAtUtc)
    {
        if (organizationId == Guid.Empty) throw new ValidationAppException("OrganizationId cannot be empty.");
        if (roleId == Guid.Empty) throw new ValidationAppException("RoleId cannot be empty.");
        if (invitedBy == Guid.Empty) throw new ValidationAppException("InvitedBy cannot be empty.");
        var guardedEmail = GuardEmail(email);
        var hash = GuardTokenHash(tokenHash);
        var createdAt = DateTime.UtcNow;
        if (expiresAtUtc <= createdAt)
            throw new ValidationAppException("Invitation expiry must be after its creation time.");

        return new UserInvitation(0, organizationId, guardedEmail, roleId, hash, invitedBy, expiresAtUtc, createdAt);
    }

    public static UserInvitation Reconstitute(
        long id, Guid organizationId, string email, string? normalizedEmail, Guid roleId, string tokenHash,
        Guid invitedBy, DateTime expiresAt, DateTime createdAt, DateTime? acceptedAt, DateTime? revokedAt,
        string? revocationReason)
    {
        return new UserInvitation(id, organizationId, email, roleId, tokenHash, invitedBy, expiresAt, createdAt)
        {
            NormalizedEmail = normalizedEmail,
            AcceptedAt = acceptedAt,
            RevokedAt = revokedAt,
            RevocationReason = revocationReason
        };
    }

    public void AssignDatabaseId(long id)
    {
        if (Id != 0) throw new DomainException("UserInvitation Id has already been assigned.");
        if (id <= 0) throw new ValidationAppException("Id must be positive.");
        Id = id;
    }

    public void Accept()
    {
        if (IsAccepted) throw new DomainException("Invitation has already been accepted.");
        if (IsRevoked) throw new DomainException("Invitation has been revoked and cannot be accepted.");
        if (IsExpired) throw new DomainException("Invitation has expired and cannot be accepted.");
        AcceptedAt = DateTime.UtcNow;
    }

    public void Revoke(string? reason = null)
    {
        if (IsAccepted) throw new DomainException("Cannot revoke an invitation that has already been accepted.");
        if (IsRevoked) throw new DomainException("Invitation is already revoked.");
        RevokedAt = DateTime.UtcNow;
        RevocationReason = GuardReason(reason);
    }

    private static string GuardEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new ValidationAppException("Email cannot be empty.");
        var trimmed = email.Trim();
        if (trimmed.Length > IdentityLengths.User.EmailMaxLength)
            throw new ValidationAppException($"Email cannot exceed {IdentityLengths.User.EmailMaxLength} characters.");
        return trimmed;
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
