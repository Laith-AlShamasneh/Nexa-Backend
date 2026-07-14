using Application.Common.Constants;
using Application.Features.Budget;
using Application.Features.Budget.Jobs;
using Infrastructure.Jobs;

namespace Infrastructure.Jobs.Handlers;

internal sealed class ComputeBudgetSnapshotHandler(
    IBudgetComputationService budgetService) : JobHandlerBase<ComputeBudgetSnapshotPayload>
{
    public override string JobType => JobTypes.ComputeBudgetSnapshot;

    protected override Task HandleAsync(ComputeBudgetSnapshotPayload payload, CancellationToken ct) =>
        budgetService.ComputeUserBudgetSnapshotAsync(payload.UserId, payload.BudgetId, ct);
}
