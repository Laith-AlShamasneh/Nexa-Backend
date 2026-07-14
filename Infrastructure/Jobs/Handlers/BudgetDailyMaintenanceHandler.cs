using Application.Common.Constants;
using Application.Features.Budget;
using Application.Features.Budget.Jobs;
using Infrastructure.Jobs;

namespace Infrastructure.Jobs.Handlers;

internal sealed class BudgetDailyMaintenanceHandler(
    IBudgetComputationService budgetService) : JobHandlerBase<BudgetDailyMaintenancePayload>
{
    public override string JobType => JobTypes.BudgetDailyMaintenance;

    protected override Task HandleAsync(BudgetDailyMaintenancePayload payload, CancellationToken ct) =>
        budgetService.RunDailyMaintenanceAsync(payload.UserId, ct);
}
