using Application.Common.Constants;
using Application.Common.Options;
using Application.Features.Email.Jobs;
using Application.Features.Tenancy.DbModels;
using Application.Features.Tenancy.DTOs;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Domain.Identity.Entities;
using Domain.Tenancy.Entities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shared.Constants;
using Shared.Enums.System;
using Shared.Results;

namespace Application.Features.Tenancy.Services;

internal sealed class TenantOnboardingService(
    IOrganizationRegistrationRepository registrationRepository,
    IPasswordHasher                     passwordHasher,
    ITokenHasher                         tokenHasher,
    IDateTimeProvider                   dateTimeProvider,
    IFileService                        fileService,
    IStorageUtility                     storageUtility,
    IUserContext                        userContext,
    IMessageProvider                    messageProvider,
    IBackgroundJobService                backgroundJobService,
    IOptions<AuthenticationOptions>      authOptions,
    ILogger<TenantOnboardingService>    logger) : ITenantOnboardingService
{
    public async Task<ServiceResult<RegisterOrganizationResponse>> RegisterAsync(
        RegisterOrganizationRequest request, CancellationToken ct = default)
    {
        // 1. Build Domain entities first — this runs their own creation invariants
        //    (non-blank names, length limits) as a second guard beyond the FluentValidation
        //    layer, and is what actually generates the client-side Guid.CreateVersion7()
        //    identifiers this whole tenant will use (see docs/DOMAIN_MODEL.md
        //    "Dapper construction strategy").
        var organization = Organization.Create(
            name: request.OrganizationName,
            slug: SlugGenerator.FromName(request.OrganizationName),
            arabicName: request.OrganizationArabicName,
            legalName: request.OrganizationLegalName,
            arabicLegalName: request.OrganizationArabicLegalName);

        var branch = Branch.Create(
            organizationId: organization.Id,
            name: request.BranchName,
            arabicName: request.BranchArabicName,
            isMainBranch: true);

        var settings = OrganizationSettings.CreateDefault(organization.Id);
        settings.UpdateLocale(
            timeZoneId: request.TimeZoneId,
            defaultLanguageCode: request.DefaultLanguageCode,
            currencyCode: request.CurrencyCode,
            dateFormat: settings.DateFormat,
            updatedBy: null);

        var person = Person.Create(
            organizationId: organization.Id,
            firstName: request.FirstName,
            lastName: request.LastName,
            arabicFirstName: request.ArabicFirstName,
            arabicLastName: request.ArabicLastName,
            phone: request.Phone);

        // 2. CPU-bound security work happens BEFORE the database round trip
        //    (see docs/TENANT_ONBOARDING.md "Performance Requirements") — password
        //    hashing and token generation must not extend the transaction's lock time.
        var passwordHash = passwordHasher.Hash(request.Password);

        var user = User.Create(
            organizationId: organization.Id,
            username: request.Username,
            email: request.Email,
            passwordHash: passwordHash,
            personId: person.Id);

        var rawConfirmationToken = tokenHasher.GenerateRawToken();
        var confirmationTokenHash = tokenHasher.Hash(rawConfirmationToken);
        var confirmationExpiresAt = dateTimeProvider.UtcNow.AddHours(authOptions.Value.EmailConfirmationExpiryHours);

        // 2.5 Upload the logo, if provided, before the database round trip — same
        //     ordering AuthService uses for ProfileImage. On any failure past this
        //     point the uploaded file is orphaned unless explicitly cleaned up (see
        //     the failure branches below and the catch block).
        string? logoFileName = null;
        string? logoUrl = null;
        if (request.Logo is not null)
        {
            var ext = Path.GetExtension(request.Logo.FileName);
            logoFileName = $"{Guid.NewGuid()}{ext}";
            var fileKey = storageUtility.BuildFileKey(FolderPaths.OrganizationLogos, logoFileName);

            await using (var stream = request.Logo.Content)
                await fileService.UploadAsync(stream, fileKey, request.Logo.ContentType, ct, request.Logo.Length);

            (logoUrl, _) = storageUtility.BuildFilePathWithExpiration(
                FolderPaths.OrganizationLogos,
                logoFileName,
                isInternalStorage: true,
                baseUrl: userContext.RequestBaseUrl);
        }

        // 3. One atomic call — see tenant.usp_Organization_Register (migration 011).
        var dbInput = new RegisterOrganizationDbInput
        {
            OrganizationId              = organization.Id,
            OrganizationName            = organization.Name,
            OrganizationArabicName      = organization.ArabicName,
            OrganizationLegalName       = organization.LegalName,
            OrganizationArabicLegalName = organization.ArabicLegalName,
            Slug                        = organization.Slug,
            LogoUrl                     = logoUrl,
            OrganizationEmail           = request.OrganizationEmail,
            OrganizationPhone           = request.OrganizationPhone,
            OrganizationAddress         = request.OrganizationAddress,

            TimeZoneId          = settings.TimeZoneId,
            DefaultLanguageCode = settings.DefaultLanguageCode,
            CurrencyCode        = settings.CurrencyCode,

            BranchId         = branch.Id,
            BranchName       = branch.Name,
            BranchArabicName = branch.ArabicName,
            BranchPhone      = request.BranchPhone,
            BranchEmail      = request.BranchEmail,
            BranchAddress    = request.BranchAddress,

            PersonId        = person.Id,
            FirstName       = person.FirstName,
            LastName        = person.LastName,
            ArabicFirstName = person.ArabicFirstName,
            ArabicLastName  = person.ArabicLastName,
            OwnerPhone      = person.Phone,

            UserId       = user.Id,
            Username     = user.Username,
            Email        = user.Email,
            PasswordHash = passwordHash,

            EmailConfirmationTokenHash    = confirmationTokenHash,
            EmailConfirmationExpiresAtUtc = confirmationExpiresAt,

            CreatedByIp   = userContext.IpAddress,
            CorrelationId = TryParseCorrelationId(userContext.TraceId)
        };

        // No need to interpolate a correlation id into the message text — every log
        // line for this request already carries one as a structured property via
        // CorrelationIdMiddleware (see WebApi/Common/Middlewares), landing in its own
        // queryable column in the Serilog MSSqlServer sink.
        logger.LogInformation("Tenant registration started for slug {Slug}.", organization.Slug);

        RegisterOrganizationDbResult dbResult;
        try
        {
            dbResult = await registrationRepository.RegisterAsync(dbInput, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Tenant registration failed unexpectedly for slug {Slug}, correlation {CorrelationId}.",
                organization.Slug, userContext.TraceId);
            await DeleteLogoIfUploadedAsync(logoFileName, ct);
            throw;
        }

        switch (dbResult.ResultCode)
        {
            case 1:
                logger.LogWarning(
                    "Tenant registration conflict (slug already in use) for slug {Slug}.", organization.Slug);
                await DeleteLogoIfUploadedAsync(logoFileName, ct);
                return ServiceResultFactory.Failure<RegisterOrganizationResponse>(
                    InternalResponseCodes.Conflict,
                    await messageProvider.GetMessagesAsync(MessageKeys.Tenancy.RegistrationFailed, ct),
                    [await messageProvider.GetMessagesAsync(MessageKeys.Tenancy.OrganizationSlugConflict, ct)]);

            case 2:
                // Missing global role templates is a seed-data/deployment defect, not a
                // caller error — log loudly so it gets noticed and fixed.
                logger.LogCritical(
                    "Tenant registration blocked: required global role templates are missing from identity.Roles. " +
                    "Re-run Database/Migrations/008_Seed_GlobalData.sql.");
                await DeleteLogoIfUploadedAsync(logoFileName, ct);
                return ServiceResultFactory.Failure<RegisterOrganizationResponse>(
                    InternalResponseCodes.InternalServerError,
                    await messageProvider.GetMessagesAsync(MessageKeys.Tenancy.RegistrationFailed, ct),
                    [await messageProvider.GetMessagesAsync(MessageKeys.Tenancy.RoleTemplatesMissing, ct)]);

            case 0:
                break;

            default:
                await DeleteLogoIfUploadedAsync(logoFileName, ct);
                logger.LogError("Tenant registration returned unrecognized ResultCode {ResultCode}.", dbResult.ResultCode);
                return ServiceResultFactory.Failure<RegisterOrganizationResponse>(
                    InternalResponseCodes.InternalServerError,
                    await messageProvider.GetMessagesAsync(MessageKeys.Tenancy.RegistrationFailed, ct));
        }

        logger.LogInformation(
            "Tenant registration succeeded: OrganizationId {OrganizationId}, OwnerUserId {OwnerUserId}.",
            dbResult.OrganizationId, dbResult.OwnerUserId);

        // 4. Enqueue the confirmation email through the existing durable background-job
        //    pipeline. The raw token is placed in the confirmation link now and never
        //    touches storage or logs again after this point.
        //
        //    The tenant transaction has already committed at this point — a failure to
        //    enqueue the email (e.g. the background-job stored procedures not existing
        //    yet in this environment) must not turn an already-successful registration
        //    into an error response. Same non-critical-failure pattern already used by
        //    AuthService.RegisterAsync for its onboarding-initialization step.
        var confirmationLink =
            $"{authOptions.Value.ConfirmEmailBaseUrl}?token={Uri.EscapeDataString(rawConfirmationToken)}";

        try
        {
            await backgroundJobService.EnqueueAsync(
                jobType: JobTypes.EmailConfirmation,
                payload: new EmailConfirmationPayload(
                    RecipientEmail: user.Email,
                    DisplayName: person.FullName,
                    ConfirmationLink: confirmationLink,
                    Language: ResolveLanguage(request.DefaultLanguageCode)),
                ct: ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Failed to enqueue confirmation email for OrganizationId {OrganizationId}, OwnerUserId {OwnerUserId}; registration still succeeds.",
                dbResult.OrganizationId, dbResult.OwnerUserId);
        }

        var response = new RegisterOrganizationResponse(
            OrganizationId: dbResult.OrganizationId!.Value,
            MainBranchId: dbResult.BranchId!.Value,
            OwnerUserId: dbResult.OwnerUserId!.Value,
            OwnerEmail: user.Email,
            EmailConfirmationRequired: true,
            LogoUrl: logoUrl,
            CreatedAt: dbResult.CreatedAt!.Value);

        return ServiceResultFactory.Success(
            response,
            InternalResponseCodes.Created,
            await messageProvider.GetMessagesAsync(MessageKeys.Tenancy.RegistrationSucceeded, ct));
    }

    private static SystemLanguages ResolveLanguage(string defaultLanguageCode) =>
        defaultLanguageCode.StartsWith("ar", StringComparison.OrdinalIgnoreCase)
            ? SystemLanguages.Arabic
            : SystemLanguages.English;

    private static Guid? TryParseCorrelationId(string? traceId) =>
        Guid.TryParse(traceId, out var id) ? id : null;

    private async Task DeleteLogoIfUploadedAsync(string? logoFileName, CancellationToken ct)
    {
        if (logoFileName is null) return;
        await fileService.DeleteAsync(storageUtility.BuildFileKey(FolderPaths.OrganizationLogos, logoFileName), ct);
    }
}
