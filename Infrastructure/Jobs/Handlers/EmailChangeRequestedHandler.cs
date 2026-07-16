using Application.Common.Constants;
using Application.Features.Email.Jobs;
using Application.Interfaces.Services;

namespace Infrastructure.Jobs.Handlers;

internal sealed class EmailChangeRequestedHandler(
    IEmailService         emailService,
    IEmailTemplateService templateService) : JobHandlerBase<EmailChangeRequestedPayload>
{
    public override string JobType => JobTypes.EmailChangeRequested;

    protected override async Task HandleAsync(EmailChangeRequestedPayload payload, CancellationToken ct)
    {
        var placeholders = new Dictionary<string, string>
        {
            ["DisplayName"]      = payload.DisplayName,
            ["ConfirmationLink"] = payload.ConfirmationLink,
            ["OldEmail"]         = payload.OldEmail,
            ["NewEmail"]         = payload.RecipientEmail,
            ["PrimaryButtonUrl"] = payload.ConfirmationLink
        };

        var (subject, htmlBody, plainTextBody) = await templateService.RenderAsync(
            JobTypes.EmailChangeRequested, payload.Language, placeholders, ct);

        await emailService.SendAsync(payload.RecipientEmail, subject, htmlBody, plainTextBody, ct: ct);
    }
}
