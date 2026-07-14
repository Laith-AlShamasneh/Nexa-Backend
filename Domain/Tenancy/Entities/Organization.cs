using Domain.Common;
using Domain.Exceptions;
using Domain.Tenancy.Constants;
using Domain.Tenancy.Enums;

namespace Domain.Tenancy.Entities;

/// <summary>
/// The tenant. Every other tenant-owned entity's <c>OrganizationId</c> points here —
/// this is the root of the tenant-isolation model (see docs/MULTI_TENANCY.md).
/// Aggregate root: owns no child collections in-memory (Dapper does not hydrate a
/// graph), but is the authority for organization-level invariants (status, trial,
/// profile). <see cref="Tenancy.Entities.Branch"/> and
/// <see cref="Tenancy.Entities.OrganizationSettings"/> are separate aggregate roots
/// that merely reference this Id — see docs/DOMAIN_MODEL.md.
/// </summary>
public sealed class Organization : Entity<Guid>, ISoftDeletable, IAuditable
{
    public string Name { get; private set; }
    public string? ArabicName { get; private set; }
    public string? LegalName { get; private set; }
    public string? ArabicLegalName { get; private set; }
    public string Slug { get; private set; }
    public string? LogoUrl { get; private set; }
    public string? Email { get; private set; }
    public string? Phone { get; private set; }
    public string? Address { get; private set; }
    public OrganizationStatus Status { get; private set; }
    public string? SubscriptionPlanCode { get; private set; }
    public DateTime? TrialEndsAt { get; private set; }

    public DateTime CreatedAt { get; private set; }
    public Guid? CreatedBy { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public Guid? UpdatedBy { get; private set; }

    public bool IsDeleted { get; private set; }
    public DateTime? DeletedAt { get; private set; }
    public Guid? DeletedBy { get; private set; }

    /// <summary>SQL Server ROWVERSION; opaque outside Infrastructure's optimistic-concurrency checks.</summary>
    public byte[]? RowVersion { get; private set; }

    private Organization(Guid id, string name, string slug, DateTime createdAt, Guid? createdBy) : base(id)
    {
        Name = name;
        Slug = slug;
        Status = OrganizationStatus.Trial;
        CreatedAt = createdAt;
        CreatedBy = createdBy;
    }

    /// <summary>Creates a brand-new organization in Trial status. Id is generated here, not by the database.</summary>
    public static Organization Create(string name, string slug, string? arabicName = null,
        string? legalName = null, string? arabicLegalName = null, Guid? createdBy = null)
    {
        var trimmedName = GuardNameLength(GuardNotBlank(name, nameof(name)));
        var trimmedSlug = GuardSlugLength(GuardNotBlank(slug, nameof(slug)));

        return new Organization(Guid.CreateVersion7(), trimmedName, trimmedSlug, DateTime.UtcNow, createdBy)
        {
            ArabicName = GuardArabicNameLength(arabicName),
            LegalName = legalName,
            ArabicLegalName = arabicLegalName
        };
    }

    /// <summary>Rebuilds an Organization from persisted state. Infrastructure-only; performs no re-validation.</summary>
    public static Organization Reconstitute(
        Guid id, string name, string? arabicName, string? legalName, string? arabicLegalName, string slug,
        string? logoUrl, string? email, string? phone, string? address, OrganizationStatus status,
        string? subscriptionPlanCode, DateTime? trialEndsAt, DateTime createdAt, Guid? createdBy,
        DateTime? updatedAt, Guid? updatedBy, bool isDeleted, DateTime? deletedAt, Guid? deletedBy, byte[]? rowVersion)
    {
        var org = new Organization(id, name, slug, createdAt, createdBy)
        {
            ArabicName = arabicName,
            LegalName = legalName,
            ArabicLegalName = arabicLegalName,
            LogoUrl = logoUrl,
            Email = email,
            Phone = phone,
            Address = address,
            Status = status,
            SubscriptionPlanCode = subscriptionPlanCode,
            TrialEndsAt = trialEndsAt,
            UpdatedAt = updatedAt,
            UpdatedBy = updatedBy,
            IsDeleted = isDeleted,
            DeletedAt = deletedAt,
            DeletedBy = deletedBy,
            RowVersion = rowVersion
        };
        return org;
    }

    public void UpdateProfile(string name, string? arabicName, string? legalName, string? arabicLegalName,
        string? logoUrl, string? email, string? phone, string? address, Guid? updatedBy)
    {
        EnsureNotDeleted();
        Name = GuardNameLength(GuardNotBlank(name, nameof(name)));
        ArabicName = GuardArabicNameLength(arabicName);
        LegalName = legalName;
        ArabicLegalName = arabicLegalName;
        LogoUrl = logoUrl;
        Email = email;
        Phone = phone;
        Address = address;
        Touch(updatedBy);
    }

    public void UpdateSubscriptionPlan(string? subscriptionPlanCode, Guid? updatedBy)
    {
        EnsureNotDeleted();
        SubscriptionPlanCode = subscriptionPlanCode;
        Touch(updatedBy);
    }

    public void StartTrial(DateTime trialEndsAtUtc, Guid? updatedBy)
    {
        EnsureNotDeleted();
        if (trialEndsAtUtc < CreatedAt)
            throw new ValidationAppException("Trial end date cannot precede the organization's creation date.");

        TrialEndsAt = trialEndsAtUtc;
        Touch(updatedBy);
    }

    public void Activate(Guid? updatedBy)
    {
        EnsureNotDeleted();
        Status = OrganizationStatus.Active;
        Touch(updatedBy);
    }

    public void Suspend(Guid? updatedBy)
    {
        EnsureNotDeleted();
        Status = OrganizationStatus.Suspended;
        Touch(updatedBy);
    }

    public void Cancel(Guid? updatedBy)
    {
        EnsureNotDeleted();
        Status = OrganizationStatus.Cancelled;
        Touch(updatedBy);
    }

    public void SoftDelete(Guid? deletedBy)
    {
        if (IsDeleted) throw new DomainException("Organization is already deleted.");
        IsDeleted = true;
        DeletedAt = DateTime.UtcNow;
        DeletedBy = deletedBy;
    }

    public void Restore()
    {
        if (!IsDeleted) throw new DomainException("Organization is not deleted.");
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
        if (IsDeleted) throw new DomainException("Cannot modify a deleted organization.");
    }

    private static string GuardNotBlank(string value, string paramName)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ValidationAppException($"{paramName} cannot be empty.");
        return value.Trim();
    }

    private static string GuardNameLength(string value)
    {
        if (value.Length > TenancyLengths.Organization.NameMaxLength)
            throw new ValidationAppException($"Organization name cannot exceed {TenancyLengths.Organization.NameMaxLength} characters.");
        return value;
    }

    private static string GuardSlugLength(string value)
    {
        if (value.Length > TenancyLengths.Organization.SlugMaxLength)
            throw new ValidationAppException($"Organization slug cannot exceed {TenancyLengths.Organization.SlugMaxLength} characters.");
        return value;
    }

    private static string? GuardArabicNameLength(string? value)
    {
        if (value is { Length: > 0 } && value.Length > TenancyLengths.Organization.NameMaxLength)
            throw new ValidationAppException($"Arabic organization name cannot exceed {TenancyLengths.Organization.NameMaxLength} characters.");
        return value;
    }
}
