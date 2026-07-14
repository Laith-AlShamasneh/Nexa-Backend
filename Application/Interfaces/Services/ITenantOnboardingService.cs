using Application.Features.Tenancy.DTOs;
using Shared.Results;

namespace Application.Interfaces.Services;

/// <summary>
/// The Tenant Onboarding use case: registers a brand-new Organization together with
/// its main Branch, settings, owner Person/User, cloned tenant roles, and an email
/// confirmation token — all as one atomic operation. See docs/TENANT_ONBOARDING.md.
/// </summary>
public interface ITenantOnboardingService
{
    Task<ServiceResult<RegisterOrganizationResponse>> RegisterAsync(
        RegisterOrganizationRequest request, CancellationToken ct = default);
}
