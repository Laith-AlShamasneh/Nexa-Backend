namespace Domain.Common;

/// <summary>
/// Marks an entity that records who created/last updated it and when. All
/// timestamps are UTC, matching the database's <c>SYSUTCDATETIME()</c> defaults.
/// </summary>
public interface IAuditable
{
    DateTime CreatedAt { get; }
    Guid? CreatedBy { get; }
    DateTime? UpdatedAt { get; }
    Guid? UpdatedBy { get; }
}
