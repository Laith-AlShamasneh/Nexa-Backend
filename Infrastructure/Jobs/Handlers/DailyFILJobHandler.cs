using Application.Common.Constants;
using Application.Features.FinancialIntelligence;
using Application.Features.FinancialIntelligence.Jobs;
using Infrastructure.Jobs;

namespace Infrastructure.Jobs.Handlers;

internal sealed class DailyFILJobHandler(
    IFILBackgroundProcessingService filService) : JobHandlerBase<DailyFILPayload>
{
    public override string JobType => JobTypes.DailyFILProcessing;

    protected override Task HandleAsync(DailyFILPayload payload, CancellationToken ct) =>
        filService.ProcessDailyAsync(payload.Year, payload.Month, payload.Day, ct);
}
