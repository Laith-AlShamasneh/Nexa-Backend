using Application.Features.Tenancy.DTOs;
using Application.Interfaces.Services;
using FluentValidation;
using Shared.Enums.System;
using Shared.Responses;
using WebApi.Common;

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
            .RequireRateLimiting(RateLimiterPolicies.PublicRegistration);

        return app;
    }

    private static async Task<IResult> RegisterAsync(
        RegisterOrganizationRequest request,
        IValidator<RegisterOrganizationRequest> validator,
        ITenantOnboardingService onboardingService,
        CancellationToken ct)
    {
        var validation = await validator.ValidateAsync(request, ct);
        if (!validation.IsValid)
        {
            var errors = validation.Errors.Select(e => e.ErrorMessage).ToList();
            var failed = ApiResponse<object?>.Fail(StatusCodes.Status400BadRequest, "Validation failed.", errors);
            return Results.Ok(failed);
        }

        var result = await onboardingService.RegisterAsync(request, ct);
        return result.ToHttpResult();
    }
}
