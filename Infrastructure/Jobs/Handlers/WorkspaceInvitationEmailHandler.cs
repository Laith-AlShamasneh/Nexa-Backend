using Application.Common.Constants;
using Application.Features.Email.Jobs;
using Application.Interfaces.Services;
using Shared.Enums.System;

namespace Infrastructure.Jobs.Handlers;

internal sealed class WorkspaceInvitationEmailHandler(
    IEmailService         emailService,
    IEmailTemplateService templateService) : JobHandlerBase<WorkspaceInvitationEmailPayload>
{
    public override string JobType => JobTypes.WorkspaceInvitationEmail;

    protected override async Task HandleAsync(WorkspaceInvitationEmailPayload payload, CancellationToken ct)
    {
        var isAr = string.Equals(payload.Language, "ar", StringComparison.OrdinalIgnoreCase);
        var lang  = isAr ? SystemLanguages.Arabic : SystemLanguages.English;

        var placeholders = new Dictionary<string, string>
        {
            ["InviterName"]   = isAr ? payload.InviterNameAr : payload.InviterNameEn,
            ["WorkspaceName"] = payload.WorkspaceName,
            ["RoleCode"]      = payload.RoleCode,
            ["AcceptLink"]    = payload.AcceptLink,
            ["ExpiresAt"]     = payload.ExpiresAtUtc.ToString("yyyy-MM-dd HH:mm UTC"),
            ["CurrentYear"]   = DateTime.UtcNow.Year.ToString()
        };

        var (subject, htmlBody) = await templateService.RenderAsync(
            JobTypes.WorkspaceInvitationEmail, lang, placeholders, ct);

        await emailService.SendAsync(payload.ToEmail, subject, htmlBody, ct: ct);
    }
}
