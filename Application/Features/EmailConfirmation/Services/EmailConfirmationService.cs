using Application.Common.Constants;
using Application.Common.Options;
using Application.Features.Email.Jobs;
using Application.Features.EmailConfirmation.DbModels;
using Application.Features.EmailConfirmation.DTOs;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shared.Constants;
using Shared.Enums.System;
using Shared.Results;

namespace Application.Features.EmailConfirmation.Services;

/// <summary>
/// Confirm + resend for the tenant-onboarding email-confirmation flow — see
/// docs/EMAIL_CONFIRMATION.md for the full design. Deliberately separate from
/// <c>AuthService</c> (Login/Register/etc. are out of scope here; see that file's
/// own comment on why its old confirm/resend methods were removed rather than kept
/// as a second, conflicting implementation).
/// </summary>
internal sealed class EmailConfirmationService(
    IEmailConfirmationRepository     repository,
    ITokenHasher                     tokenHasher,
    IDateTimeProvider                dateTimeProvider,
    IUserContext                     userContext,
    IMessageProvider                 messageProvider,
    IBackgroundJobService             backgroundJobService,
    IOptions<AuthenticationOptions>   authOptions,
    IOptions<EmailConfirmationOptions> confirmationOptions,
    ILogger<EmailConfirmationService> logger) : IEmailConfirmationService
{
    public async Task<ServiceResult<ConfirmEmailResponse>> ConfirmAsync(
        ConfirmEmailRequest request, CancellationToken ct = default)
    {
        // Raw token never persisted, never logged — only its hash leaves this method.
        var tokenHash = tokenHasher.Hash(request.Token);

        var dbResult = await repository.ConfirmAsync(new ConfirmEmailDbInput
        {
            TokenHash     = tokenHash,
            UsedByIp      = userContext.IpAddress,
            CorrelationId = TryParseCorrelationId(userContext.TraceId)
        }, ct);

        switch (dbResult.ResultCode)
        {
            case 0:
                logger.LogInformation("Email confirmed for UserId {UserId}.", dbResult.UserId);
                return ServiceResultFactory.Success(
                    new ConfirmEmailResponse(true),
                    InternalResponseCodes.OK,
                    await messageProvider.GetMessagesAsync(MessageKeys.Authentication.EmailConfirmed, ct));

            case 1:
                // Idempotent — token already used, or the user was already confirmed.
                // Same success shape as case 0; the client cannot tell them apart.
                return ServiceResultFactory.Success(
                    new ConfirmEmailResponse(true),
                    InternalResponseCodes.OK,
                    await messageProvider.GetMessagesAsync(MessageKeys.Authentication.EmailAlreadyConfirmed, ct));

            default:
                // Not found, expired, revoked, or the user/organization isn't
                // eligible — one generic failure, no distinguishing detail returned.
                logger.LogWarning("Email confirmation rejected (invalid, expired, or revoked token).");
                return ServiceResultFactory.Failure<ConfirmEmailResponse>(
                    InternalResponseCodes.BadRequest,
                    await messageProvider.GetMessagesAsync(MessageKeys.Authentication.InvalidToken, ct));
        }
    }

    public async Task<ServiceResult<ResendEmailConfirmationResponse>> ResendAsync(
        ResendEmailConfirmationRequest request, CancellationToken ct = default)
    {
        // No-enumeration: this exact response is returned regardless of whether the
        // account exists, is already confirmed, is inactive, or hit a cooldown/rate
        // limit — every branch below returns it.
        var genericMessage = await messageProvider.GetMessagesAsync(MessageKeys.Authentication.ConfirmationEmailSent, ct);
        var genericSuccess = ServiceResultFactory.Success(
            new ResendEmailConfirmationResponse(), InternalResponseCodes.OK, genericMessage);

        // Generated unconditionally, before we know whether it'll be used — hashing
        // is pure/local and doesn't leak anything by happening speculatively; the SP
        // decides whether this hash is actually worth persisting.
        var rawToken  = tokenHasher.GenerateRawToken();
        var tokenHash = tokenHasher.Hash(rawToken);
        var expiresAt = dateTimeProvider.UtcNow.AddHours(authOptions.Value.EmailConfirmationExpiryHours);

        var dbResult = await repository.ResendAsync(new ResendEmailConfirmationDbInput
        {
            Email                 = request.Email,
            NewTokenHash          = tokenHash,
            NewTokenExpiresAtUtc  = expiresAt,
            ResendCooldownSeconds = confirmationOptions.Value.ResendCooldownSeconds,
            MaxResendsPerHour     = confirmationOptions.Value.MaxResendsPerHour,
            RequestIp             = userContext.IpAddress,
            CorrelationId         = TryParseCorrelationId(userContext.TraceId)
        }, ct);

        if (dbResult.ResultCode != 0)
        {
            // NotEligible / CooldownActive / MaxResendsPerHourExceeded — the raw
            // token generated above was never persisted (its hash never reached a
            // row), so there is nothing to clean up; just discard it.
            return genericSuccess;
        }

        var confirmationLink = $"{authOptions.Value.ConfirmEmailBaseUrl}?token={Uri.EscapeDataString(rawToken)}";
        var displayName = ResolveDisplayName(dbResult, userContext.Language);

        try
        {
            await backgroundJobService.EnqueueAsync(
                jobType: JobTypes.EmailConfirmation,
                payload: new EmailConfirmationPayload(request.Email, displayName, confirmationLink, userContext.Language),
                ct: ct);
        }
        catch (Exception ex)
        {
            // Token creation already committed — per docs/EMAIL_CONFIRMATION.md
            // "Email delivery failure strategy", we do not roll that back or treat
            // this as a caller-visible error. Same non-critical-failure pattern
            // TenantOnboardingService already uses for its own confirmation email.
            logger.LogError(ex,
                "Failed to enqueue resend confirmation email for UserId {UserId}; token was already created.",
                dbResult.UserId);
        }

        return genericSuccess;
    }

    private static string ResolveDisplayName(ResendEmailConfirmationDbResult dbResult, SystemLanguages language) =>
        language.IsRightToLeft() && !string.IsNullOrWhiteSpace(dbResult.DisplayNameAr)
            ? dbResult.DisplayNameAr!
            : dbResult.DisplayNameEn ?? string.Empty;

    private static Guid? TryParseCorrelationId(string? traceId) =>
        Guid.TryParse(traceId, out var id) ? id : null;
}
