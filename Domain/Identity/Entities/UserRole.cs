using Domain.Exceptions;

namespace Domain.Identity.Entities;

/// <summary>
/// The User↔Role assignment. Modeled as an immutable record rather than an
/// <see cref="Entity{TId}"/>: it has a composite key (UserId, RoleId), not a single
/// scalar identity, and nothing about an assignment changes after it's made — to
/// revoke it, the row is deleted, not updated. <see cref="OrganizationId"/> is
/// denormalized from both <see cref="User"/> and <see cref="Role"/> (which must
/// agree — enforced by composite tenant-safe foreign keys in the database) purely so
/// tenant-scoped queries don't need a join to filter by it.
/// </summary>
public sealed record UserRole
{
    public Guid UserId { get; }
    public Guid RoleId { get; }
    public Guid OrganizationId { get; }
    public DateTime AssignedAt { get; }
    public Guid? AssignedBy { get; }

    private UserRole(Guid userId, Guid roleId, Guid organizationId, DateTime assignedAt, Guid? assignedBy)
    {
        UserId = userId;
        RoleId = roleId;
        OrganizationId = organizationId;
        AssignedAt = assignedAt;
        AssignedBy = assignedBy;
    }

    public static UserRole Assign(Guid userId, Guid roleId, Guid organizationId, Guid? assignedBy = null)
    {
        if (userId == Guid.Empty) throw new ValidationAppException("UserId cannot be empty.");
        if (roleId == Guid.Empty) throw new ValidationAppException("RoleId cannot be empty.");
        if (organizationId == Guid.Empty) throw new ValidationAppException("OrganizationId cannot be empty.");

        return new UserRole(userId, roleId, organizationId, DateTime.UtcNow, assignedBy);
    }

    public static UserRole Reconstitute(Guid userId, Guid roleId, Guid organizationId, DateTime assignedAt,
        Guid? assignedBy) =>
        new(userId, roleId, organizationId, assignedAt, assignedBy);
}
