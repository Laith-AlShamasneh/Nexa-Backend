namespace Domain.Customers.Enums;

/// <summary>
/// Mirrors <c>crm.Customers.Status</c> (TINYINT, CK_Customers_Status restricts to
/// 0/1/2). Ordinal values are the database's stored values.
/// </summary>
public enum CustomerStatus : byte
{
    Inactive = 0,
    Active   = 1,
    Archived = 2
}
