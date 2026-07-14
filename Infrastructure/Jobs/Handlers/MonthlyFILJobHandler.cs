using Application.Common.Constants;
using Application.Features.FinancialIntelligence;
using Application.Features.FinancialIntelligence.Jobs;
using Infrastructure.Jobs;

namespace Infrastructure.Jobs.Handlers;

internal sealed class MonthlyFILJobHandler(
    IFILBackgroundProcessingService filService) : JobHandlerBase<MonthlyFILPayload>
{
    public override string JobType => JobTypes.MonthlyFILProcessing;

    protected override Task HandleAsync(MonthlyFILPayload payload, CancellationToken ct) =>
        filService.ProcessMonthlyAsync(payload.Year, payload.Month, ct);
}
