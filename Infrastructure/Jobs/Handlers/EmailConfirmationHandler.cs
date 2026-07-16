using Application.Common.Constants;
using Application.Features.Email.Jobs;
using Application.Interfaces.Services;

namespace Infrastructure.Jobs.Handlers;

internal sealed class EmailConfirmationHandler(
    IEmailService         emailService,
    IEmailTemplateService templateService) : JobHandlerBase<EmailConfirmationPayload>
{
    public override string JobType => JobTypes.EmailConfirmation;

    protected override async Task HandleAsync(EmailConfirmationPayload payload, CancellationToken ct)
    {
        var placeholders = new Dictionary<string, string>
        {
            ["DisplayName"]      = payload.DisplayName,
            ["ConfirmationLink"] = payload.ConfirmationLink,
            // The base layout's CTA button is driven by PrimaryButtonUrl/Text — Url is
            // per-send dynamic data, so it's set here rather than in the template files
            // (Text comes from meta.json, static copy — see EmailTemplateService).
            ["PrimaryButtonUrl"] = payload.ConfirmationLink
        };

        var (subject, htmlBody, plainTextBody) = await templateService.RenderAsync(
            JobTypes.EmailConfirmation, payload.Language, placeholders, ct);

        await emailService.SendAsync(payload.RecipientEmail, subject, htmlBody, plainTextBody, ct: ct);
    }
}
