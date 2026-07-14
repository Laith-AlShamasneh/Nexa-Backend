using Domain.Common;
using Domain.Exceptions;

namespace Domain.Customers.Entities;

/// <summary>
/// A free-text note attached to a <see cref="Customer"/>. Supporting entity, not an
/// aggregate root — it always exists in the context of its owning Customer.
/// <see cref="CreatedBy"/> is required (unlike most other entities' optional
/// CreatedBy) because the schema requires it: a note always has an author. Gained
/// <see cref="DeletedAt"/>/<see cref="DeletedBy"/> in migration 009 (upgraded from a
/// bare IsDeleted flag to full soft-delete-with-actor). The schema has no
/// <c>UpdatedAt</c> column, so editing a note does not currently record when it
/// changed — a documented gap, not an oversight of this model.
/// </summary>
public sealed class CustomerNote : Entity<long>, ITenantOwned, ISoftDeletable
{
    public Guid OrganizationId { get; }
    public Guid CustomerId { get; }
    public string Note { get; private set; }
    public Guid CreatedBy { get; }
    public DateTime CreatedAt { get; }

    public bool IsDeleted { get; private set; }
    public DateTime? DeletedAt { get; private set; }
    public Guid? DeletedBy { get; private set; }

    private CustomerNote(long id, Guid organizationId, Guid customerId, string note, Guid createdBy, DateTime createdAt)
        : base(id)
    {
        OrganizationId = organizationId;
        CustomerId = customerId;
        Note = note;
        CreatedBy = createdBy;
        CreatedAt = createdAt;
    }

    public static CustomerNote Create(Guid organizationId, Guid customerId, string note, Guid createdBy)
    {
        if (organizationId == Guid.Empty) throw new ValidationAppException("OrganizationId cannot be empty.");
        if (customerId == Guid.Empty) throw new ValidationAppException("CustomerId cannot be empty.");
        if (createdBy == Guid.Empty) throw new ValidationAppException("CreatedBy cannot be empty.");

        return new CustomerNote(0, organizationId, customerId, GuardNote(note), createdBy, DateTime.UtcNow);
    }

    public static CustomerNote Reconstitute(
        long id, Guid organizationId, Guid customerId, string note, Guid createdBy, DateTime createdAt,
        bool isDeleted, DateTime? deletedAt, Guid? deletedBy)
    {
        return new CustomerNote(id, organizationId, customerId, note, createdBy, createdAt)
        {
            IsDeleted = isDeleted,
            DeletedAt = deletedAt,
            DeletedBy = deletedBy
        };
    }

    public void AssignDatabaseId(long id)
    {
        if (Id != 0) throw new DomainException("CustomerNote Id has already been assigned.");
        if (id <= 0) throw new ValidationAppException("Id must be positive.");
        Id = id;
    }

    public void UpdateNote(string note)
    {
        EnsureNotDeleted();
        Note = GuardNote(note);
    }

    public void SoftDelete(Guid? deletedBy)
    {
        if (IsDeleted) throw new DomainException("Customer note is already deleted.");
        IsDeleted = true;
        DeletedAt = DateTime.UtcNow;
        DeletedBy = deletedBy;
    }

    public void Restore()
    {
        if (!IsDeleted) throw new DomainException("Customer note is not deleted.");
        IsDeleted = false;
        DeletedAt = null;
        DeletedBy = null;
    }

    private void EnsureNotDeleted()
    {
        if (IsDeleted) throw new DomainException("Cannot modify a deleted customer note.");
    }

    private static string GuardNote(string note)
    {
        if (string.IsNullOrWhiteSpace(note))
            throw new ValidationAppException("Note cannot be empty.");
        return note.Trim();
    }
}
