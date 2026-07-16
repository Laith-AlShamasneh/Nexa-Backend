using Application.Common.Constants;
using Application.Features.Email.Jobs;
using Application.Interfaces.Services;

namespace Infrastructure.Jobs.Handlers;

internal sealed class WelcomeEmailHandler(
    IEmailService         emailService,
    IEmailTemplateService templateService) : JobHandlerBase<WelcomeEmailPayload>
{
    public override string JobType => JobTypes.WelcomeEmail;

    protected override async Task HandleAsync(WelcomeEmailPayload payload, CancellationToken ct)
    {
        var placeholders = new Dictionary<string, string>
        {
            ["DisplayName"] = payload.DisplayName,
            ["Email"]       = payload.RecipientEmail
        };

        var (subject, htmlBody, plainTextBody) = await templateService.RenderAsync(
            JobTypes.WelcomeEmail, payload.Language, placeholders, ct);

        await emailService.SendAsync(payload.RecipientEmail, subject, htmlBody, plainTextBody, ct: ct);
    }
}
