using Application.Common.Constants;
using Application.Features.Reports.Jobs;
using Application.Interfaces.Services;
using Infrastructure.Jobs;
using Shared.Enums.System;

namespace Infrastructure.Jobs.Handlers;

internal sealed class ReportCompletedEmailHandler(
    IEmailService         emailService,
    IEmailTemplateService templateService) : JobHandlerBase<ReportCompletedEmailPayload>
{
    public override string JobType => JobTypes.ReportCompletedEmail;

    protected override async Task HandleAsync(ReportCompletedEmailPayload payload, CancellationToken ct)
    {
        bool ar   = payload.Language == "ar";
        var  lang = ar ? SystemLanguages.Arabic : SystemLanguages.English;
        var  name = ar ? payload.ReportTypeNameAr : payload.ReportTypeNameEn;

        var placeholders = new Dictionary<string, string>
        {
            ["DisplayName"]     = payload.UserDisplayName,
            ["ReportTypeName"]  = name,
            ["DateFrom"]        = payload.DateFrom,
            ["DateTo"]          = payload.DateTo,
            ["GeneratedOn"]     = payload.GeneratedOn,
            ["CurrentYear"]     = DateTime.UtcNow.Year.ToString()
        };

        var (subject, htmlBody) = await templateService.RenderAsync(
            JobTypes.ReportCompletedEmail, lang, placeholders, ct);

        await emailService.SendAsync(payload.To, subject, htmlBody, ct: ct);
    }
}
