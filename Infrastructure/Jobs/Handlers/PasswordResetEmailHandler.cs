using Application.Common.Constants;
using Application.Features.Email.Jobs;
using Application.Interfaces.Services;

namespace Infrastructure.Jobs.Handlers;

internal sealed class PasswordResetEmailHandler(
    IEmailService         emailService,
    IEmailTemplateService templateService) : JobHandlerBase<PasswordResetEmailPayload>
{
    public override string JobType => JobTypes.PasswordResetEmail;

    protected override async Task HandleAsync(PasswordResetEmailPayload payload, CancellationToken ct)
    {
        var placeholders = new Dictionary<string, string>
        {
            ["DisplayName"]      = payload.DisplayName,
            ["ResetLink"]        = payload.ResetLink,
            ["PrimaryButtonUrl"] = payload.ResetLink
        };

        var (subject, htmlBody, plainTextBody) = await templateService.RenderAsync(
            JobTypes.PasswordResetEmail, payload.Language, placeholders, ct);

        await emailService.SendAsync(payload.RecipientEmail, subject, htmlBody, plainTextBody, ct: ct);
    }
}
