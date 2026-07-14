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
            ["DisplayName"]       = payload.DisplayName,
            ["ConfirmationLink"]  = payload.ConfirmationLink,
            ["OldEmail"]          = payload.OldEmail,
            ["NewEmail"]          = payload.RecipientEmail,
            ["CurrentYear"]       = DateTime.UtcNow.Year.ToString()
        };

        var (subject, htmlBody) = await templateService.RenderAsync(
            JobTypes.EmailChangeRequested, payload.Language, placeholders, ct);

        await emailService.SendAsync(payload.RecipientEmail, subject, htmlBody, ct: ct);
    }
}
