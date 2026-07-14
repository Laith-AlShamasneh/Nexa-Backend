using Application.Common.Constants;
using Application.Features.RecurringTransactions;
using Application.Features.RecurringTransactions.Jobs;
using Infrastructure.Jobs;

namespace Infrastructure.Jobs.Handlers;

internal sealed class SendUpcomingPaymentNotificationHandler(
    IRecurringTransactionEngineService engineService) : JobHandlerBase<SendUpcomingPaymentNotificationPayload>
{
    public override string JobType => JobTypes.SendUpcomingPaymentNotification;

    protected override Task HandleAsync(SendUpcomingPaymentNotificationPayload payload, CancellationToken ct) =>
        engineService.SendUpcomingNotificationsAsync(payload.DaysAhead, ct);
}
