namespace Domain.Tenancy.Enums;

/// <summary>
/// Mirrors <c>tenant.Branches.Status</c> (TINYINT, CK_Branches_Status restricts
/// to 0/1). Ordinal values are the database's stored values.
/// </summary>
public enum BranchStatus : byte
{
    Inactive = 0,
    Active   = 1
}
