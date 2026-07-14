namespace Domain.Common;

/// <summary>
/// Marks an entity as belonging to exactly one organization for its entire lifetime.
/// <see cref="OrganizationId"/> is set once at creation and never changed afterward —
/// there is deliberately no <c>ChangeOrganization</c>/setter anywhere in the model
/// (see docs/MULTI_TENANCY.md). Not every tenant-scoped table implements this: rows
/// whose <c>OrganizationId</c> is legitimately nullable (global role templates,
/// platform-level audit/sign-in events) expose the column directly instead — see
/// docs/DOMAIN_MODEL.md for the full list of exceptions and why.
/// </summary>
public interface ITenantOwned
{
    Guid OrganizationId { get; }
}
