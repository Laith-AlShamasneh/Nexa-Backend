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
        var isAr = string.Equals(payload.Language, "ar", StringComparison.OrdinalIgnoreCase);
        var lang  = isAr ? SystemLanguages.Arabic : SystemLanguages.English;

        var placeholders = new Dictionary<string, string>
        {
            ["InviterName"]      = isAr ? payload.InviterNameAr : payload.InviterNameEn,
            ["OrganizationName"] = payload.OrganizationName,
            ["RoleName"]         = payload.RoleName,
            ["AcceptLink"]       = payload.AcceptLink,
            ["ExpiresAt"]        = payload.ExpiresAtUtc.ToString("yyyy-MM-dd HH:mm UTC"),
            ["CurrentYear"]      = DateTime.UtcNow.Year.ToString()
        };

        var (subject, htmlBody) = await templateService.RenderAsync(
            JobTypes.OrganizationInvitationEmail, lang, placeholders, ct);

        await emailService.SendAsync(payload.ToEmail, subject, htmlBody, ct: ct);
    }
}
