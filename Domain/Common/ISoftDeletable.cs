namespace Domain.Common;

/// <summary>
/// Marks an entity that is deactivated by flag rather than removed. All timestamps
/// are UTC. <see cref="IsDeleted"/>/<see cref="DeletedAt"/> are always consistent
/// with each other (mirrors each table's <c>CK_*_SoftDelete</c> database constraint).
/// </summary>
public interface ISoftDeletable
{
    bool IsDeleted { get; }
    DateTime? DeletedAt { get; }
    Guid? DeletedBy { get; }
}
