using Application.Common.Constants;
using Application.Features.Email.Jobs;
using Application.Interfaces.Services;

namespace Infrastructure.Jobs.Handlers;

internal sealed class PasswordChangedEmailHandler(
    IEmailService         emailService,
    IEmailTemplateService templateService) : JobHandlerBase<PasswordChangedEmailPayload>
{
    public override string JobType => JobTypes.PasswordChangedEmail;

    protected override async Task HandleAsync(PasswordChangedEmailPayload payload, CancellationToken ct)
    {
        var placeholders = new Dictionary<string, string>
        {
            ["DisplayName"] = payload.DisplayName,
            ["ChangeTime"]  = payload.ChangeTime,
            ["CurrentYear"] = DateTime.UtcNow.Year.ToString()
        };

        var (subject, htmlBody) = await templateService.RenderAsync(
            JobTypes.PasswordChangedEmail, payload.Language, placeholders, ct);

        await emailService.SendAsync(payload.RecipientEmail, subject, htmlBody, ct: ct);
    }
}
