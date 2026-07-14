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
            ["CurrentYear"]      = DateTime.UtcNow.Year.ToString()
        };

        var (subject, htmlBody) = await templateService.RenderAsync(
            JobTypes.EmailConfirmation, payload.Language, placeholders, ct);

        await emailService.SendAsync(payload.RecipientEmail, subject, htmlBody, ct: ct);
    }
}
