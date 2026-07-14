using Application.Features.Tenancy.DbModels;

namespace Application.Interfaces.Repositories;

/// <summary>
/// Executes the single atomic tenant-onboarding transaction
/// (<c>tenant.usp_Organization_Register</c>). One call performs every write —
/// Organization, Branch, OrganizationSettings, Person, User, cloned tenant Roles and
/// RolePermissions, the Owner role assignment, the email-confirmation token, and the
/// audit log rows — or none of them.
/// </summary>
public interface IOrganizationRegistrationRepository
{
    Task<RegisterOrganizationDbResult> RegisterAsync(
        RegisterOrganizationDbInput input, CancellationToken ct = default);
}
