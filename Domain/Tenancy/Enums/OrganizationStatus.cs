namespace Domain.Tenancy.Enums;

/// <summary>
/// Mirrors <c>tenant.Organizations.Status</c> (TINYINT, CK_Organizations_Status
/// restricts to 0-3). Ordinal values are the database's stored values — do not
/// renumber without a corresponding migration.
/// </summary>
public enum OrganizationStatus : byte
{
    Trial     = 0,
    Active    = 1,
    Suspended = 2,
    Cancelled = 3
}
