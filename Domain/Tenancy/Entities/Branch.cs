using Domain.Common;
using Domain.Exceptions;
using Domain.Tenancy.Constants;
using Domain.Tenancy.Enums;

namespace Domain.Tenancy.Entities;

/// <summary>
/// A physical/operational location within an <see cref="Organization"/>.
/// </summary>
/// <remarks>
/// <b>Cross-aggregate invariant (not enforced here):</b> at most one active Branch per
/// organization may have <see cref="IsMainBranch"/> = true. A single Branch instance
/// cannot check its siblings, so this rule is enforced by
/// <c>UX_Branches_Organization_MainBranch</c> (filtered unique index) at the database
/// layer and must be respected by the Application workflow that calls
/// <see cref="SetAsMainBranch"/> (e.g. demote the current main branch first). See
/// docs/DOMAIN_MODEL.md.
/// </remarks>
public sealed class Branch : Entity<Guid>, ITenantOwned, ISoftDeletable, IAuditable
{
    public Guid OrganizationId { get; }
    public string Name { get; private set; }
    public string? ArabicName { get; private set; }
    public string? Code { get; private set; }
    public string? Address { get; private set; }
    public string? Phone { get; private set; }
    public string? Email { get; private set; }
    public bool IsMainBranch { get; private set; }
    public BranchStatus Status { get; private set; }

    public DateTime CreatedAt { get; private set; }
    public Guid? CreatedBy { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public Guid? UpdatedBy { get; private set; }

    public bool IsDeleted { get; private set; }
    public DateTime? DeletedAt { get; private set; }
    public Guid? DeletedBy { get; private set; }

    public byte[]? RowVersion { get; private set; }

    private Branch(Guid id, Guid organizationId, string name, bool isMainBranch, DateTime createdAt, Guid? createdBy)
        : base(id)
    {
        OrganizationId = organizationId;
        Name = name;
        IsMainBranch = isMainBranch;
        Status = BranchStatus.Active;
        CreatedAt = createdAt;
        CreatedBy = createdBy;
    }

    public static Branch Create(Guid organizationId, string name, string? arabicName = null,
        bool isMainBranch = false, Guid? createdBy = null)
    {
        if (organizationId == Guid.Empty)
            throw new ValidationAppException("OrganizationId cannot be empty.");

        var trimmedName = GuardName(name);
        return new Branch(Guid.CreateVersion7(), organizationId, trimmedName, isMainBranch, DateTime.UtcNow, createdBy)
        {
            ArabicName = GuardArabicName(arabicName)
        };
    }

    public static Branch Reconstitute(
        Guid id, Guid organizationId, string name, string? arabicName, string? code, string? address, string? phone,
        string? email, bool isMainBranch, BranchStatus status, DateTime createdAt, Guid? createdBy,
        DateTime? updatedAt, Guid? updatedBy, bool isDeleted, DateTime? deletedAt, Guid? deletedBy, byte[]? rowVersion)
    {
        return new Branch(id, organizationId, name, isMainBranch, createdAt, createdBy)
        {
            ArabicName = arabicName,
            Code = code,
            Address = address,
            Phone = phone,
            Email = email,
            Status = status,
            UpdatedAt = updatedAt,
            UpdatedBy = updatedBy,
            IsDeleted = isDeleted,
            DeletedAt = deletedAt,
            DeletedBy = deletedBy,
            RowVersion = rowVersion
        };
    }

    public void UpdateDetails(string name, string? arabicName, string? code, Guid? updatedBy)
    {
        EnsureNotDeleted();
        Name = GuardName(name);
        ArabicName = GuardArabicName(arabicName);
        Code = code;
        Touch(updatedBy);
    }

    public void UpdateContactInfo(string? address, string? phone, string? email, Guid? updatedBy)
    {
        EnsureNotDeleted();
        Address = address;
        Phone = phone;
        Email = email;
        Touch(updatedBy);
    }

    /// <summary>Marks this branch as the organization's main branch. See the cross-aggregate invariant above.</summary>
    public void SetAsMainBranch(Guid? updatedBy)
    {
        EnsureNotDeleted();
        IsMainBranch = true;
        Touch(updatedBy);
    }

    public void UnsetAsMainBranch(Guid? updatedBy)
    {
        EnsureNotDeleted();
        IsMainBranch = false;
        Touch(updatedBy);
    }

    public void Activate(Guid? updatedBy)
    {
        EnsureNotDeleted();
        Status = BranchStatus.Active;
        Touch(updatedBy);
    }

    public void Deactivate(Guid? updatedBy)
    {
        EnsureNotDeleted();
        Status = BranchStatus.Inactive;
        Touch(updatedBy);
    }

    public void SoftDelete(Guid? deletedBy)
    {
        if (IsDeleted) throw new DomainException("Branch is already deleted.");
        IsDeleted = true;
        DeletedAt = DateTime.UtcNow;
        DeletedBy = deletedBy;
    }

    public void Restore()
    {
        if (!IsDeleted) throw new DomainException("Branch is not deleted.");
        IsDeleted = false;
        DeletedAt = null;
        DeletedBy = null;
    }

    private void Touch(Guid? updatedBy)
    {
        UpdatedAt = DateTime.UtcNow;
        UpdatedBy = updatedBy;
    }

    private void EnsureNotDeleted()
    {
        if (IsDeleted) throw new DomainException("Cannot modify a deleted branch.");
    }

    private static string GuardName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ValidationAppException("Branch name cannot be empty.");
        var trimmed = name.Trim();
        if (trimmed.Length > TenancyLengths.Branch.NameMaxLength)
            throw new ValidationAppException($"Branch name cannot exceed {TenancyLengths.Branch.NameMaxLength} characters.");
        return trimmed;
    }

    private static string? GuardArabicName(string? arabicName)
    {
        if (arabicName is { Length: > 0 } && arabicName.Length > TenancyLengths.Branch.NameMaxLength)
            throw new ValidationAppException($"Arabic branch name cannot exceed {TenancyLengths.Branch.NameMaxLength} characters.");
        return arabicName;
    }
}
