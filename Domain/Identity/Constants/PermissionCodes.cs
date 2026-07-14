namespace Domain.Identity.Constants;

/// <summary>
/// Stable identifiers for the permission catalog seeded by
/// Database/Migrations/008_Seed_GlobalData.sql. Keep this list in sync with that
/// script — these strings are what <see cref="Entities.Permission.Code"/> actually
/// stores, and what Application-layer authorization checks compare against.
/// </summary>
public static class PermissionCodes
{
    public static class Organization
    {
        public const string View = "Organization.View";
        public const string Manage = "Organization.Manage";
    }

    public static class Branch
    {
        public const string View = "Branch.View";
        public const string Manage = "Branch.Manage";
    }

    public static class User
    {
        public const string View = "User.View";
        public const string Manage = "User.Manage";
    }

    public static class Role
    {
        public const string Manage = "Role.Manage";
    }

    public static class Customer
    {
        public const string View = "Customer.View";
        public const string Create = "Customer.Create";
        public const string Update = "Customer.Update";
        public const string Delete = "Customer.Delete";
    }

    public static class Payment
    {
        public const string View = "Payment.View";
        public const string Create = "Payment.Create";
    }

    public static class Report
    {
        public const string View = "Report.View";
    }

    public static class Attendance
    {
        public const string View = "Attendance.View";
        public const string Record = "Attendance.Record";
    }
}
