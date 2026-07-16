using Application.Features.EmailConfirmation.DTOs;
using Application.Interfaces.Services;
using WebApi.Common;
using WebApi.Common.Filters;

namespace WebApi.Endpoints.Authentication;

/// <summary>
/// Confirm + resend for the email-confirmation token every new organization owner
/// receives at registration (see docs/TENANT_ONBOARDING.md and
/// docs/EMAIL_CONFIRMATION.md). Both endpoints are public/pre-authentication — there
/// is no JWT yet for an unconfirmed user — so, like tenant registration, they are a
/// documented exception to "tenant context always comes from the JWT"
/// (see docs/MULTI_TENANCY.md). Neither endpoint accepts an OrganizationId or UserId
/// from the client; both are resolved entirely from the token/email server-side.
/// </summary>
public static class EmailConfirmationEndpoints
{
    public static IEndpointRouteBuilder MapEmailConfirmationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth").WithTags("Email Confirmation");

        group.MapPost("/confirm-email", ConfirmEmailAsync)
            .WithName("ConfirmEmail")
            .RequireRateLimiting(RateLimiterPolicies.PublicEmailConfirmation)
            .AddEndpointFilter<ValidationFilter<ConfirmEmailRequest>>();

        group.MapPost("/resend-email-confirmation", ResendEmailConfirmationAsync)
            .WithName("ResendEmailConfirmation")
            .RequireRateLimiting(RateLimiterPolicies.PublicEmailConfirmation)
            .AddEndpointFilter<ValidationFilter<ResendEmailConfirmationRequest>>();

        return app;
    }

    private static async Task<IResult> ConfirmEmailAsync(
        ConfirmEmailRequest request,
        IEmailConfirmationService emailConfirmationService,
        CancellationToken ct)
    {
        var result = await emailConfirmationService.ConfirmAsync(request, ct);
        return result.ToHttpResult();
    }

    private static async Task<IResult> ResendEmailConfirmationAsync(
        ResendEmailConfirmationRequest request,
        IEmailConfirmationService emailConfirmationService,
        CancellationToken ct)
    {
        var result = await emailConfirmationService.ResendAsync(request, ct);
        return result.ToHttpResult();
    }
}
