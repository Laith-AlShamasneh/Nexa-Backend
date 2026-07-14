using Domain.Common;
using Domain.Customers.Constants;
using Domain.Customers.Enums;
using Domain.Exceptions;

namespace Domain.Customers.Entities;

/// <summary>
/// The Core CRM entity — deliberately named Customer, not Student: the education
/// vertical displays this as "Student" in its UI, a future clinic vertical would
/// display it as "Patient", but the Core Domain stays vertical-neutral (see
/// PRODUCT_CONTEXT.md and docs/database/DATABASE_FINAL_BLUEPRINT.md). Course,
/// Enrollment, Attendance, and Payment concepts do not belong on this entity — they
/// are future vertical-specific additions that will reference this Id, not extend it.
/// </summary>
public sealed class Customer : Entity<Guid>, ITenantOwned, ISoftDeletable, IAuditable
{
    public Guid OrganizationId { get; }
    public Guid? PersonId { get; private set; }
    public string? CustomerCode { get; private set; }
    public string CustomerType { get; private set; }
    public string? DisplayName { get; private set; }
    public CustomerStatus Status { get; private set; }
    public string? Source { get; private set; }

    public DateTime CreatedAt { get; private set; }
    public Guid? CreatedBy { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public Guid? UpdatedBy { get; private set; }

    public bool IsDeleted { get; private set; }
    public DateTime? DeletedAt { get; private set; }
    public Guid? DeletedBy { get; private set; }

    public byte[]? RowVersion { get; private set; }

    private Customer(Guid id, Guid organizationId, string customerType, DateTime createdAt, Guid? createdBy)
        : base(id)
    {
        OrganizationId = organizationId;
        CustomerType = customerType;
        Status = CustomerStatus.Active;
        CreatedAt = createdAt;
        CreatedBy = createdBy;
    }

    public static Customer Create(Guid organizationId, string customerType, string? displayName = null,
        Guid? personId = null, string? customerCode = null, string? source = null, Guid? createdBy = null)
    {
        if (organizationId == Guid.Empty)
            throw new ValidationAppException("OrganizationId cannot be empty.");

        var customer = new Customer(Guid.CreateVersion7(), organizationId, GuardCustomerType(customerType),
            DateTime.UtcNow, createdBy)
        {
            PersonId = personId,
            CustomerCode = GuardOptionalLength(customerCode, CustomerLengths.CustomerCodeMaxLength, nameof(customerCode)),
            DisplayName = GuardOptionalLength(displayName, CustomerLengths.DisplayNameMaxLength, nameof(displayName)),
            Source = GuardOptionalLength(source, CustomerLengths.SourceMaxLength, nameof(source))
        };
        return customer;
    }

    public static Customer Reconstitute(
        Guid id, Guid organizationId, Guid? personId, string? customerCode, string customerType,
        string? displayName, CustomerStatus status, string? source, DateTime createdAt, Guid? createdBy,
        DateTime? updatedAt, Guid? updatedBy, bool isDeleted, DateTime? deletedAt, Guid? deletedBy, byte[]? rowVersion)
    {
        return new Customer(id, organizationId, customerType, createdAt, createdBy)
        {
            PersonId = personId,
            CustomerCode = customerCode,
            DisplayName = displayName,
            Status = status,
            Source = source,
            UpdatedAt = updatedAt,
            UpdatedBy = updatedBy,
            IsDeleted = isDeleted,
            DeletedAt = deletedAt,
            DeletedBy = deletedBy,
            RowVersion = rowVersion
        };
    }

    public void UpdateCustomerType(string customerType, Guid? updatedBy)
    {
        EnsureNotDeleted();
        CustomerType = GuardCustomerType(customerType);
        Touch(updatedBy);
    }

    public void UpdateDisplayName(string? displayName, Guid? updatedBy)
    {
        EnsureNotDeleted();
        DisplayName = GuardOptionalLength(displayName, CustomerLengths.DisplayNameMaxLength, nameof(displayName));
        Touch(updatedBy);
    }

    public void Activate(Guid? updatedBy)
    {
        EnsureNotDeleted();
        Status = CustomerStatus.Active;
        Touch(updatedBy);
    }

    public void Deactivate(Guid? updatedBy)
    {
        EnsureNotDeleted();
        Status = CustomerStatus.Inactive;
        Touch(updatedBy);
    }

    public void Archive(Guid? updatedBy)
    {
        EnsureNotDeleted();
        Status = CustomerStatus.Archived;
        Touch(updatedBy);
    }

    public void SoftDelete(Guid? deletedBy)
    {
        if (IsDeleted) throw new DomainException("Customer is already deleted.");
        IsDeleted = true;
        DeletedAt = DateTime.UtcNow;
        DeletedBy = deletedBy;
    }

    public void Restore()
    {
        if (!IsDeleted) throw new DomainException("Customer is not deleted.");
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
        if (IsDeleted) throw new DomainException("Cannot modify a deleted customer.");
    }

    private static string GuardCustomerType(string customerType)
    {
        if (string.IsNullOrWhiteSpace(customerType))
            throw new ValidationAppException("CustomerType cannot be empty.");
        var trimmed = customerType.Trim();
        if (trimmed.Length > CustomerLengths.CustomerTypeMaxLength)
            throw new ValidationAppException($"CustomerType cannot exceed {CustomerLengths.CustomerTypeMaxLength} characters.");
        return trimmed;
    }

    private static string? GuardOptionalLength(string? value, int maxLength, string paramName)
    {
        if (value is { Length: > 0 } && value.Length > maxLength)
            throw new ValidationAppException($"{paramName} cannot exceed {maxLength} characters.");
        return value;
    }
}
