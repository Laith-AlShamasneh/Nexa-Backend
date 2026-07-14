using Application.Common.Constants;
using Application.Features.CashFlow;
using Application.Features.CashFlow.Jobs;
using Infrastructure.Jobs;

namespace Infrastructure.Jobs.Handlers;

internal sealed class ComputeForecastHandler(
    ICashFlowComputationService cashFlowService) : JobHandlerBase<ComputeForecastPayload>
{
    public override string JobType => JobTypes.ComputeCashFlowForecast;

    protected override Task HandleAsync(ComputeForecastPayload payload, CancellationToken ct) =>
        cashFlowService.ProcessUserForecastAsync(payload.UserId, payload.WorkspaceId, ct);
}
