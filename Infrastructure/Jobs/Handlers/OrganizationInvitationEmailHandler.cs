using Application.Common.Constants;
using Application.Features.Email.Jobs;
using Application.Interfaces.Services;
using Shared.Enums.System;

namespace Infrastructure.Jobs.Handlers;

internal sealed class OrganizationInvitationEmailHandler(
    IEmailService         emailService,
    IEmailTemplateService templateService) : JobHandlerBase<OrganizationInvitationEmailPayload>
{
    public override string JobType => JobTypes.OrganizationInvitationEmail;

    protected override async Task HandleAsync(OrganizationInvitationEmailPayload payload, CancellationToken ct)
    {
        var lang = SystemLanguageExtensions.FromLanguageCode(payload.Language);

        var placeholders = new Dictionary<string, string>
        {
            ["InviterName"]      = lang.IsRightToLeft() ? payload.InviterNameAr : payload.InviterNameEn,
            ["OrganizationName"] = payload.OrganizationName,
            ["RoleName"]         = payload.RoleName,
            ["AcceptLink"]       = payload.AcceptLink,
            ["ExpiresAt"]        = payload.ExpiresAtUtc.ToString("yyyy-MM-dd HH:mm UTC"),
            ["PrimaryButtonUrl"] = payload.AcceptLink
        };

        var (subject, htmlBody, plainTextBody) = await templateService.RenderAsync(
            JobTypes.OrganizationInvitationEmail, lang, placeholders, ct);

        await emailService.SendAsync(payload.ToEmail, subject, htmlBody, plainTextBody, ct: ct);
    }
}
