using Application.Common.Constants;
using Application.Features.Email.Jobs;
using Application.Interfaces.Services;

namespace Infrastructure.Jobs.Handlers;

internal sealed class EmailChangedHandler(
    IEmailService         emailService,
    IEmailTemplateService templateService) : JobHandlerBase<EmailChangedPayload>
{
    public override string JobType => JobTypes.EmailChanged;

    protected override async Task HandleAsync(EmailChangedPayload payload, CancellationToken ct)
    {
        var placeholders = new Dictionary<string, string>
        {
            ["DisplayName"] = payload.DisplayName,
            ["NewEmail"]    = payload.NewEmail,
            ["ChangeTime"]  = payload.ChangeTime
        };

        var (subject, htmlBody, plainTextBody) = await templateService.RenderAsync(
            JobTypes.EmailChanged, payload.Language, placeholders, ct);

        await emailService.SendAsync(payload.RecipientEmail, subject, htmlBody, plainTextBody, ct: ct);
    }
}
