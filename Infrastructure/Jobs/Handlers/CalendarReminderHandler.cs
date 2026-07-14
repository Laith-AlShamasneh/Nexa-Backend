using Application.Common.Constants;
using Application.Features.Calendar.Jobs;
using Application.Features.Notifications.Jobs;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;

namespace Infrastructure.Jobs.Handlers;

internal sealed class CalendarReminderHandler(
    ICalendarRepository   calendarRepository,
    IBackgroundJobService backgroundJobService) : JobHandlerBase<CalendarReminderPayload>
{
    public override string JobType => JobTypes.CalendarReminder;

    protected override async Task HandleAsync(CalendarReminderPayload payload, CancellationToken ct)
    {
        // Deep-link payload so the Notification Center item (and the reminder popup)
        // can open the event directly. JSON is intentionally minimal + stable.
        var deepLink = System.Text.Json.JsonSerializer.Serialize(new
        {
            code       = "CALENDAR_REMINDER_DUE",
            eventId    = payload.EventId,
            reminderId = payload.ReminderId,
        });

        var notificationPayload = new CreateNotificationPayload(
            TemplateCode: "CALENDAR_REMINDER_DUE",
            UserId:       payload.UserId,
            Parameters: new Dictionary<string, string>
            {
                { "EventTitle", payload.EventTitle },
                { "EventDate",  payload.EventDate  },
            },
            PayloadJson: deepLink);

        await backgroundJobService.EnqueueAsync(
            JobTypes.CreateNotification,
            notificationPayload,
            priority: 2,
            ct: ct);

        await calendarRepository.MarkReminderSentAsync(payload.ReminderId, jobId: null, ct);
    }
}
