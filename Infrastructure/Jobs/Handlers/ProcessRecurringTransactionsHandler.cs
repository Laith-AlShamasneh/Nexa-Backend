using Application.Common.Constants;
using Application.Features.RecurringTransactions;
using Application.Features.RecurringTransactions.Jobs;
using Infrastructure.Jobs;

namespace Infrastructure.Jobs.Handlers;

internal sealed class ProcessRecurringTransactionsHandler(
    IRecurringTransactionEngineService engineService) : JobHandlerBase<ProcessRecurringTransactionsPayload>
{
    public override string JobType => JobTypes.ProcessRecurringTransactions;

    protected override Task HandleAsync(ProcessRecurringTransactionsPayload payload, CancellationToken ct) =>
        engineService.ProcessDueTransactionsAsync(payload.ProcessingDate, ct);
}
