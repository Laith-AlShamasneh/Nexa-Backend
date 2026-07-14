using Application.Features.Tenancy.DTOs;
using Application.Interfaces.Services;
using WebApi.Common;
using WebApi.Common.Filters;

namespace WebApi.Endpoints.Tenancy;

/// <summary>
/// Tenant Onboarding's public surface — see docs/TENANT_ONBOARDING.md. This is the
/// one endpoint in the platform that runs before any tenant exists, so it is the
/// documented exception to "OrganizationId always comes from the authenticated
/// context" (see docs/MULTI_TENANCY.md "Pre-authentication tenant creation").
/// </summary>
public static class OrganizationEndpoints
{
    public static IEndpointRouteBuilder MapOrganizationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/organizations").WithTags("Organizations");

        group.MapPost("/register", RegisterAsync)
            .WithName("RegisterOrganization")
            .RequireRateLimiting(RateLimiterPolicies.PublicRegistration)
            .AddEndpointFilter<ValidationFilter<RegisterOrganizationRequest>>();

        return app;
    }

    private static async Task<IResult> RegisterAsync(
        RegisterOrganizationRequest request,
        ITenantOnboardingService onboardingService,
        CancellationToken ct)
    {
        var result = await onboardingService.RegisterAsync(request, ct);
        return result.ToHttpResult();
    }
}
