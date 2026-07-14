using Domain.Common;
using Domain.Exceptions;
using Domain.Identity.Constants;
using Shared.Enums.Identity;

namespace Domain.Identity.Entities;

/// <summary>
/// A real human being's personal information — deliberately separate from
/// <see cref="User"/> (authentication). A Person need not have login access
/// (e.g. a Customer contact with no system account); a User need not be linked to a
/// Person (e.g. a service account) — see docs/MULTI_TENANCY.md and
/// docs/DOMAIN_MODEL.md. No authentication behavior belongs here.
/// </summary>
public sealed class Person : Entity<Guid>, ITenantOwned, ISoftDeletable, IAuditable
{
    public Guid OrganizationId { get; }
    public string FirstName { get; private set; }
    public string LastName { get; private set; }
    public string? ArabicFirstName { get; private set; }
    public string? ArabicLastName { get; private set; }

    /// <summary>Display convenience only — not the database's computed `FullName` column value; both derive the same trimmed concatenation.</summary>
    public string FullName => $"{FirstName} {LastName}".Trim();

    /// <summary>
    /// Display convenience mirroring the database's computed `ArabicFullName` column:
    /// null when neither Arabic name part is set (not an empty string), otherwise the
    /// trimmed concatenation of whichever parts are present.
    /// </summary>
    public string? ArabicFullName =>
        ArabicFirstName is null && ArabicLastName is null
            ? null
            : $"{ArabicFirstName} {ArabicLastName}".Trim();

    public string? Email { get; private set; }
    public string? Phone { get; private set; }
    public DateOnly? DateOfBirth { get; private set; }
    public GenderTypes? Gender { get; private set; }
    public string? ProfileImageUrl { get; private set; }

    public DateTime CreatedAt { get; private set; }
    public Guid? CreatedBy { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public Guid? UpdatedBy { get; private set; }

    public bool IsDeleted { get; private set; }
    public DateTime? DeletedAt { get; private set; }
    public Guid? DeletedBy { get; private set; }

    public byte[]? RowVersion { get; private set; }

    private Person(Guid id, Guid organizationId, string firstName, string lastName, DateTime createdAt, Guid? createdBy)
        : base(id)
    {
        OrganizationId = organizationId;
        FirstName = firstName;
        LastName = lastName;
        CreatedAt = createdAt;
        CreatedBy = createdBy;
    }

    public static Person Create(Guid organizationId, string firstName, string lastName, string? arabicFirstName = null,
        string? arabicLastName = null, string? email = null, string? phone = null, DateOnly? dateOfBirth = null,
        GenderTypes? gender = null, Guid? createdBy = null)
    {
        if (organizationId == Guid.Empty)
            throw new ValidationAppException("OrganizationId cannot be empty.");

        var person = new Person(Guid.CreateVersion7(), organizationId, GuardFirstName(firstName),
            GuardLastName(lastName), DateTime.UtcNow, createdBy)
        {
            ArabicFirstName = GuardArabicFirstName(arabicFirstName),
            ArabicLastName = GuardArabicLastName(arabicLastName),
            Email = email,
            Phone = phone,
            DateOfBirth = dateOfBirth,
            Gender = gender
        };
        return person;
    }

    public static Person Reconstitute(
        Guid id, Guid organizationId, string firstName, string lastName, string? arabicFirstName,
        string? arabicLastName, string? email, string? phone, DateOnly? dateOfBirth, GenderTypes? gender,
        string? profileImageUrl, DateTime createdAt, Guid? createdBy, DateTime? updatedAt, Guid? updatedBy,
        bool isDeleted, DateTime? deletedAt, Guid? deletedBy, byte[]? rowVersion)
    {
        return new Person(id, organizationId, firstName, lastName, createdAt, createdBy)
        {
            ArabicFirstName = arabicFirstName,
            ArabicLastName = arabicLastName,
            Email = email,
            Phone = phone,
            DateOfBirth = dateOfBirth,
            Gender = gender,
            ProfileImageUrl = profileImageUrl,
            UpdatedAt = updatedAt,
            UpdatedBy = updatedBy,
            IsDeleted = isDeleted,
            DeletedAt = deletedAt,
            DeletedBy = deletedBy,
            RowVersion = rowVersion
        };
    }

    public void UpdatePersonalDetails(string firstName, string lastName, string? arabicFirstName,
        string? arabicLastName, DateOnly? dateOfBirth, GenderTypes? gender, Guid? updatedBy)
    {
        EnsureNotDeleted();
        FirstName = GuardFirstName(firstName);
        LastName = GuardLastName(lastName);
        ArabicFirstName = GuardArabicFirstName(arabicFirstName);
        ArabicLastName = GuardArabicLastName(arabicLastName);
        DateOfBirth = dateOfBirth;
        Gender = gender;
        Touch(updatedBy);
    }

    public void UpdateContactDetails(string? email, string? phone, Guid? updatedBy)
    {
        EnsureNotDeleted();
        Email = email;
        Phone = phone;
        Touch(updatedBy);
    }

    public void UpdateProfileImage(string? profileImageUrl, Guid? updatedBy)
    {
        EnsureNotDeleted();
        ProfileImageUrl = profileImageUrl;
        Touch(updatedBy);
    }

    public void SoftDelete(Guid? deletedBy)
    {
        if (IsDeleted) throw new DomainException("Person is already deleted.");
        IsDeleted = true;
        DeletedAt = DateTime.UtcNow;
        DeletedBy = deletedBy;
    }

    public void Restore()
    {
        if (!IsDeleted) throw new DomainException("Person is not deleted.");
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
        if (IsDeleted) throw new DomainException("Cannot modify a deleted person.");
    }

    private static string GuardFirstName(string value) => GuardName(value, IdentityLengths.Person.FirstNameMaxLength, "First name");
    private static string GuardLastName(string value) => GuardName(value, IdentityLengths.Person.LastNameMaxLength, "Last name");

    private static string GuardName(string value, int maxLength, string label)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ValidationAppException($"{label} cannot be empty.");
        var trimmed = value.Trim();
        if (trimmed.Length > maxLength)
            throw new ValidationAppException($"{label} cannot exceed {maxLength} characters.");
        return trimmed;
    }

    private static string? GuardArabicFirstName(string? value) => GuardOptionalName(value, IdentityLengths.Person.FirstNameMaxLength, "Arabic first name");
    private static string? GuardArabicLastName(string? value) => GuardOptionalName(value, IdentityLengths.Person.LastNameMaxLength, "Arabic last name");

    private static string? GuardOptionalName(string? value, int maxLength, string label)
    {
        if (value is not { Length: > 0 }) return value;
        if (value.Length > maxLength)
            throw new ValidationAppException($"{label} cannot exceed {maxLength} characters.");
        return value;
    }
}
