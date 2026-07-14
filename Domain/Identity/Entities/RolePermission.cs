using Domain.Exceptions;

namespace Domain.Identity.Entities;

/// <summary>
/// The Role↔Permission grant. Immutable record with a composite key
/// (RoleId, PermissionId) — a role either has a permission or it doesn't; there is
/// no partial/updatable state to a grant. Revoking a permission from a role deletes
/// the row.
/// </summary>
public sealed record RolePermission
{
    public Guid RoleId { get; }
    public int PermissionId { get; }
    public DateTime CreatedAt { get; }

    private RolePermission(Guid roleId, int permissionId, DateTime createdAt)
    {
        RoleId = roleId;
        PermissionId = permissionId;
        CreatedAt = createdAt;
    }

    public static RolePermission Grant(Guid roleId, int permissionId)
    {
        if (roleId == Guid.Empty) throw new ValidationAppException("RoleId cannot be empty.");
        return new RolePermission(roleId, permissionId, DateTime.UtcNow);
    }

    public static RolePermission Reconstitute(Guid roleId, int permissionId, DateTime createdAt) =>
        new(roleId, permissionId, createdAt);
}
