using Domain.Common;

namespace Domain.Identity.Entities;

/// <summary>
/// A single fine-grained permission (e.g. "Customer.View" — see
/// <see cref="Constants.PermissionCodes"/>). This is a read-only catalog: rows are
/// seeded once by Database/Migrations/008_Seed_GlobalData.sql, never created or
/// edited through application code, so this entity exposes no mutation behavior —
/// only <see cref="Reconstitute"/> for reading. <see cref="Code"/> is the stable
/// identifier; <see cref="Id"/> is only a database surrogate key.
/// </summary>
public sealed class Permission : Entity<int>
{
    public string Code { get; }
    public string Name { get; }
    public string? ArabicName { get; }
    public string? Description { get; }
    public string? Module { get; }
    public DateTime CreatedAt { get; }

    private Permission(int id, string code, string name, string? arabicName, string? description, string? module,
        DateTime createdAt) : base(id)
    {
        Code = code;
        Name = name;
        ArabicName = arabicName;
        Description = description;
        Module = module;
        CreatedAt = createdAt;
    }

    public static Permission Reconstitute(int id, string code, string name, string? arabicName, string? description,
        string? module, DateTime createdAt) =>
        new(id, code, name, arabicName, description, module, createdAt);
}
