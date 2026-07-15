using Application.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;
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

        // multipart/form-data, not JSON — RegisterOrganizationFormRequest carries an
        // optional Logo file. The generic ValidationFilter<TRequest> can't be used
        // here: it inspects already-bound minimal-API arguments for TRequest, but the
        // bound argument is RegisterOrganizationFormRequest (the WebApi transport
        // type), not RegisterOrganizationRequest (the Application type the validator
        // targets) — so validation runs manually inside the handler after mapping.
        group.MapPost("/register", RegisterAsync)
            .WithName("RegisterOrganization")
            .Accepts<RegisterOrganizationFormRequest>("multipart/form-data")
            .RequireRateLimiting(RateLimiterPolicies.PublicRegistration)
            // ASP.NET Core auto-attaches antiforgery metadata to any minimal-API
            // endpoint bound from a form containing a file, and there is no
            // antiforgery middleware registered (this is a stateless, unauthenticated
            // API — no ambient cookie session for CSRF to exploit; the rate limiter
            // above is this endpoint's actual anti-abuse control). Without this call
            // the endpoint throws on every request — see
            // Microsoft.AspNetCore.Routing.EndpointMiddleware's antiforgery check.
            .DisableAntiforgery();

        return app;
    }

    private static async Task<IResult> RegisterAsync(
        [FromForm] RegisterOrganizationFormRequest form,
        HttpContext httpContext,
        ITenantOnboardingService onboardingService,
        IMessageProvider messageProvider,
        CancellationToken ct)
    {
        var request = form.ToApplicationRequest();

        var validationFailure = await RequestValidator.ValidateAsync(
            httpContext.RequestServices, request, messageProvider, ct);
        if (validationFailure is not null) return validationFailure;

        var result = await onboardingService.RegisterAsync(request, ct);
        return result.ToHttpResult();
    }
}
