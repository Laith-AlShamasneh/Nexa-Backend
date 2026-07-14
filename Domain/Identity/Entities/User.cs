using Domain.Common;
using Domain.Exceptions;
using Domain.Identity.Constants;

namespace Domain.Identity.Entities;

/// <summary>
/// An authentication account. Optionally linked to a <see cref="Person"/>
/// (<see cref="PersonId"/> is nullable — a service/API account need not be a human).
/// Domain manages only the *resulting state* of security operations: password
/// hashing, JWT issuance, and token generation all happen in Infrastructure and are
/// handed to this entity as already-computed values (a hash, a stamp) — see
/// docs/SECURITY_BASELINE.md.
/// </summary>
/// <remarks>
/// <see cref="NormalizedEmail"/>/<see cref="NormalizedUsername"/> mirror the
/// database's computed columns (<c>UPPER(LTRIM(RTRIM(...)))</c>) and are populated
/// only by <see cref="Reconstitute"/> — Domain does not recompute them, to avoid the
/// normalization logic drifting from the database's authoritative version (see
/// docs/DOMAIN_MODEL.md).
/// </remarks>
public sealed class User : Entity<Guid>, ITenantOwned, ISoftDeletable, IAuditable
{
    public Guid OrganizationId { get; }
    public Guid? PersonId { get; private set; }
    public string Username { get; private set; }
    public string? NormalizedUsername { get; private set; }
    public string Email { get; private set; }
    public string? NormalizedEmail { get; private set; }
    public string PasswordHash { get; private set; }
    public string SecurityStamp { get; private set; }
    public string ConcurrencyStamp { get; private set; }
    public bool IsEmailConfirmed { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime? LastLoginAt { get; private set; }
    public string? LastLoginIp { get; private set; }
    public int FailedLoginAttempts { get; private set; }
    public DateTime? LockoutEndDate { get; private set; }

    public DateTime CreatedAt { get; private set; }
    public Guid? CreatedBy { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public Guid? UpdatedBy { get; private set; }

    public bool IsDeleted { get; private set; }
    public DateTime? DeletedAt { get; private set; }
    public Guid? DeletedBy { get; private set; }

    public byte[]? RowVersion { get; private set; }

    public bool IsLockedOut => LockoutEndDate is { } until && until > DateTime.UtcNow;

    private User(Guid id, Guid organizationId, string username, string email, string passwordHash,
        DateTime createdAt, Guid? createdBy) : base(id)
    {
        OrganizationId = organizationId;
        Username = username;
        Email = email;
        PasswordHash = passwordHash;
        SecurityStamp = Guid.NewGuid().ToString("N");
        ConcurrencyStamp = Guid.NewGuid().ToString("N");
        CreatedAt = createdAt;
        CreatedBy = createdBy;
    }

    public static User Create(Guid organizationId, string username, string email, string passwordHash,
        Guid? personId = null, Guid? createdBy = null)
    {
        if (organizationId == Guid.Empty)
            throw new ValidationAppException("OrganizationId cannot be empty.");

        var user = new User(Guid.CreateVersion7(), organizationId, GuardUsername(username), GuardEmail(email),
            GuardPasswordHash(passwordHash), DateTime.UtcNow, createdBy)
        {
            PersonId = personId,
            IsActive = true
        };
        return user;
    }

    public static User Reconstitute(
        Guid id, Guid organizationId, Guid? personId, string username, string? normalizedUsername, string email,
        string? normalizedEmail, string passwordHash, string securityStamp, string concurrencyStamp,
        bool isEmailConfirmed, bool isActive, DateTime? lastLoginAt, string? lastLoginIp, int failedLoginAttempts,
        DateTime? lockoutEndDate, DateTime createdAt, Guid? createdBy, DateTime? updatedAt, Guid? updatedBy,
        bool isDeleted, DateTime? deletedAt, Guid? deletedBy, byte[]? rowVersion)
    {
        return new User(id, organizationId, username, email, passwordHash, createdAt, createdBy)
        {
            PersonId = personId,
            NormalizedUsername = normalizedUsername,
            NormalizedEmail = normalizedEmail,
            SecurityStamp = securityStamp,
            ConcurrencyStamp = concurrencyStamp,
            IsEmailConfirmed = isEmailConfirmed,
            IsActive = isActive,
            LastLoginAt = lastLoginAt,
            LastLoginIp = lastLoginIp,
            FailedLoginAttempts = failedLoginAttempts,
            LockoutEndDate = lockoutEndDate,
            UpdatedAt = updatedAt,
            UpdatedBy = updatedBy,
            IsDeleted = isDeleted,
            DeletedAt = deletedAt,
            DeletedBy = deletedBy,
            RowVersion = rowVersion
        };
    }

    /// <summary>True when the account is usable to authenticate — does not by itself imply email confirmation is required; callers decide that policy.</summary>
    public bool CanSignIn() => !IsDeleted && IsActive && !IsLockedOut;

    public void ConfirmEmail(Guid? updatedBy)
    {
        EnsureNotDeleted();
        IsEmailConfirmed = true;
        Touch(updatedBy);
    }

    /// <summary>Replaces the stored password hash and bumps the security stamp, invalidating already-issued tokens that assert the old stamp.</summary>
    public void ChangePasswordHash(string newPasswordHash, Guid? updatedBy)
    {
        EnsureNotDeleted();
        PasswordHash = GuardPasswordHash(newPasswordHash);
        BumpSecurityStamp();
        Touch(updatedBy);
    }

    public void ChangeEmail(string newEmail, Guid? updatedBy)
    {
        EnsureNotDeleted();
        Email = GuardEmail(newEmail);
        IsEmailConfirmed = false;
        Touch(updatedBy);
    }

    public void LinkPerson(Guid personId, Guid? updatedBy)
    {
        EnsureNotDeleted();
        if (personId == Guid.Empty)
            throw new ValidationAppException("PersonId cannot be empty.");
        PersonId = personId;
        Touch(updatedBy);
    }

    public void RecordSuccessfulSignIn(string? ipAddress)
    {
        LastLoginAt = DateTime.UtcNow;
        LastLoginIp = ipAddress;
        FailedLoginAttempts = 0;
        LockoutEndDate = null;
    }

    /// <summary>Increments the failed-attempt counter and applies the lockout policy the caller supplies (Domain does not own these thresholds — see Application's AuthenticationOptions).</summary>
    public void RecordFailedSignIn(int maxFailedAttempts, TimeSpan lockoutDuration)
    {
        FailedLoginAttempts++;
        if (FailedLoginAttempts >= maxFailedAttempts)
            LockoutEndDate = DateTime.UtcNow.Add(lockoutDuration);
    }

    public void Lock(DateTime untilUtc)
    {
        LockoutEndDate = untilUtc;
    }

    public void Unlock()
    {
        LockoutEndDate = null;
        FailedLoginAttempts = 0;
    }

    public void Activate(Guid? updatedBy)
    {
        EnsureNotDeleted();
        IsActive = true;
        Touch(updatedBy);
    }

    public void Deactivate(Guid? updatedBy)
    {
        EnsureNotDeleted();
        IsActive = false;
        BumpSecurityStamp();
        Touch(updatedBy);
    }

    public void SoftDelete(Guid? deletedBy)
    {
        if (IsDeleted) throw new DomainException("User is already deleted.");
        IsDeleted = true;
        DeletedAt = DateTime.UtcNow;
        DeletedBy = deletedBy;
        BumpSecurityStamp();
    }

    public void Restore()
    {
        if (!IsDeleted) throw new DomainException("User is not deleted.");
        IsDeleted = false;
        DeletedAt = null;
        DeletedBy = null;
    }

    private void BumpSecurityStamp() => SecurityStamp = Guid.NewGuid().ToString("N");

    private void Touch(Guid? updatedBy)
    {
        UpdatedAt = DateTime.UtcNow;
        UpdatedBy = updatedBy;
        ConcurrencyStamp = Guid.NewGuid().ToString("N");
    }

    private void EnsureNotDeleted()
    {
        if (IsDeleted) throw new DomainException("Cannot modify a deleted user.");
    }

    private static string GuardUsername(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ValidationAppException("Username cannot be empty.");
        var trimmed = value.Trim();
        if (trimmed.Length > IdentityLengths.User.UsernameMaxLength)
            throw new ValidationAppException($"Username cannot exceed {IdentityLengths.User.UsernameMaxLength} characters.");
        return trimmed;
    }

    private static string GuardEmail(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ValidationAppException("Email cannot be empty.");
        var trimmed = value.Trim();
        if (trimmed.Length > IdentityLengths.User.EmailMaxLength)
            throw new ValidationAppException($"Email cannot exceed {IdentityLengths.User.EmailMaxLength} characters.");
        return trimmed;
    }

    private static string GuardPasswordHash(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ValidationAppException("Password hash cannot be empty.");
        return value;
    }
}
