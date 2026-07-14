using Domain.Common;
using Domain.Exceptions;
using Domain.Identity.Constants;

namespace Domain.Identity.Entities;

/// <summary>
/// Either a <b>global system template</b> (<see cref="OrganizationId"/> is null,
/// never assigned to a user directly — "Owner", "Admin", "Accountant", "Teacher")
/// or a <b>tenant-local role</b> (<see cref="OrganizationId"/> set). Migration 009
/// materializes one tenant-local clone of every system template per organization,
/// linked back via <see cref="TemplateRoleId"/>; <c>UserRoles</c> may only reference
/// tenant-local roles (enforced by a composite tenant-safe FK — see
/// docs/database/DATABASE_FINAL_BLUEPRINT.md §2).
/// </summary>
/// <remarks>
/// This entity does not implement <see cref="ITenantOwned"/> because
/// <see cref="OrganizationId"/> is legitimately nullable for global templates — see
/// docs/DOMAIN_MODEL.md for the full list of such exceptions. Cross-tenant role
/// assignment (which template maps to which tenant clone, materializing new clones
/// for a new organization) is an Application workflow, not something one Role
/// instance can do by itself.
/// </remarks>
public sealed class Role : Entity<Guid>, IAuditable
{
    public Guid? OrganizationId { get; }
    public Guid? TemplateRoleId { get; }
    public string Name { get; private set; }
    public string? Description { get; private set; }
    public bool IsSystemRole { get; private set; }

    public DateTime CreatedAt { get; private set; }
    public Guid? CreatedBy { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public Guid? UpdatedBy { get; private set; }

    public bool IsDeleted { get; private set; }
    public DateTime? DeletedAt { get; private set; }
    public Guid? DeletedBy { get; private set; }

    public bool IsGlobalTemplate => OrganizationId is null;
    public bool IsTenantRole => OrganizationId is not null;

    private Role(Guid id, Guid? organizationId, Guid? templateRoleId, string name, bool isSystemRole,
        DateTime createdAt, Guid? createdBy) : base(id)
    {
        OrganizationId = organizationId;
        TemplateRoleId = templateRoleId;
        Name = name;
        IsSystemRole = isSystemRole;
        CreatedAt = createdAt;
        CreatedBy = createdBy;
    }

    /// <summary>Creates a global role template (Owner/Admin/Accountant/Teacher), shared by every tenant, never assigned directly.</summary>
    public static Role CreateSystemTemplate(string name, string? description = null, Guid? createdBy = null)
    {
        var role = new Role(Guid.CreateVersion7(), null, null, GuardName(name), true, DateTime.UtcNow, createdBy);
        role.Description = description;
        return role;
    }

    /// <summary>
    /// Creates a tenant-local role — either a clone of a global template
    /// (pass the template's Id as <paramref name="templateRoleId"/> and
    /// <paramref name="isSystemRole"/> = true) or a genuinely custom role the tenant
    /// defines for itself (<paramref name="templateRoleId"/> = null).
    /// </summary>
    public static Role CreateTenantRole(Guid organizationId, string name, string? description = null,
        bool isSystemRole = false, Guid? templateRoleId = null, Guid? createdBy = null)
    {
        if (organizationId == Guid.Empty)
            throw new ValidationAppException("OrganizationId cannot be empty.");

        var role = new Role(Guid.CreateVersion7(), organizationId, templateRoleId, GuardName(name), isSystemRole,
            DateTime.UtcNow, createdBy);
        role.Description = description;
        return role;
    }

    public static Role Reconstitute(
        Guid id, Guid? organizationId, Guid? templateRoleId, string name, string? description, bool isSystemRole,
        DateTime createdAt, Guid? createdBy, DateTime? updatedAt, Guid? updatedBy,
        bool isDeleted, DateTime? deletedAt, Guid? deletedBy)
    {
        return new Role(id, organizationId, templateRoleId, name, isSystemRole, createdAt, createdBy)
        {
            Description = description,
            UpdatedAt = updatedAt,
            UpdatedBy = updatedBy,
            IsDeleted = isDeleted,
            DeletedAt = deletedAt,
            DeletedBy = deletedBy
        };
    }

    public void Rename(string name, string? description, Guid? updatedBy)
    {
        EnsureNotDeleted();
        Name = GuardName(name);
        Description = description;
        Touch(updatedBy);
    }

    public void SoftDelete(Guid? deletedBy)
    {
        if (IsDeleted) throw new DomainException("Role is already deleted.");
        IsDeleted = true;
        DeletedAt = DateTime.UtcNow;
        DeletedBy = deletedBy;
    }

    public void Restore()
    {
        if (!IsDeleted) throw new DomainException("Role is not deleted.");
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
        if (IsDeleted) throw new DomainException("Cannot modify a deleted role.");
    }

    private static string GuardName(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ValidationAppException("Role name cannot be empty.");
        var trimmed = value.Trim();
        if (trimmed.Length > IdentityLengths.Role.NameMaxLength)
            throw new ValidationAppException($"Role name cannot exceed {IdentityLengths.Role.NameMaxLength} characters.");
        return trimmed;
    }
}
