using Application.Common.Constants;
using Application.Features.Notifications.DbModels;
using Application.Features.Notifications.Jobs;
using Application.Interfaces.Repositories;
using Infrastructure.Jobs;
using Shared.Enums.Notifications;

namespace Infrastructure.Jobs.Handlers;

internal sealed class CreateNotificationHandler(
    INotificationRepository notificationRepository) : JobHandlerBase<CreateNotificationPayload>
{
    public override string JobType => JobTypes.CreateNotification;

    protected override async Task HandleAsync(CreateNotificationPayload payload, CancellationToken ct)
    {
        var (template, translations) = await notificationRepository.GetTemplateByCodeAsync(payload.TemplateCode, ct);

        if (template is null || !template.IsActive)
            return;

        var prefs = await notificationRepository.GetOrInitPreferencesAsync(payload.UserId, ct);

        if (!IsCategoryEnabled(prefs, (NotificationCategory)template.Category))
            return;

        var en = translations.FirstOrDefault(t => t.LanguageCode == "en");
        var ar = translations.FirstOrDefault(t => t.LanguageCode == "ar");

        if (en is null || ar is null)
            return;

        // Prefer caller-supplied pre-rendered text; otherwise substitute {placeholders}
        // into the template translations.
        var titleEn   = payload.TitleEn   ?? ApplyParameters(en.TitleTemplate,   payload.Parameters);
        var titleAr   = payload.TitleAr   ?? ApplyParameters(ar.TitleTemplate,   payload.Parameters);
        var messageEn = payload.MessageEn ?? ApplyParameters(en.MessageTemplate, payload.Parameters);
        var messageAr = payload.MessageAr ?? ApplyParameters(ar.MessageTemplate, payload.Parameters);

        await notificationRepository.CreateAsync(
            new CreateNotificationDbModel
            {
                UserId      = payload.UserId,
                TemplateId  = template.TemplateId,
                Category    = template.Category,
                Type        = template.Type,
                Priority    = template.Priority,
                TitleEn     = titleEn,
                TitleAr     = titleAr,
                MessageEn   = messageEn,
                MessageAr   = messageAr,
                PayloadJson = payload.PayloadJson
            }, ct);
    }

    private static string ApplyParameters(string template, Dictionary<string, string>? parameters)
    {
        if (parameters is null || parameters.Count == 0)
            return template;

        foreach (var (key, value) in parameters)
            template = template.Replace($"{{{key}}}", value, StringComparison.OrdinalIgnoreCase);

        return template;
    }

    private static bool IsCategoryEnabled(NotificationPreferencesDbResult prefs, NotificationCategory category) =>
        category switch
        {
            NotificationCategory.Security  => prefs.SecurityEnabled,
            NotificationCategory.Billing => prefs.BillingEnabled,
            NotificationCategory.System    => prefs.SystemEnabled,
            NotificationCategory.Reports   => prefs.ReportsEnabled,
            NotificationCategory.Profile   => prefs.ProfileEnabled,
            _                              => true
        };
}
