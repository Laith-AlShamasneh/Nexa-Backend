using Application.Common.Constants;
using Application.Features.Goals;
using Application.Features.Goals.Jobs;
using Infrastructure.Jobs;

namespace Infrastructure.Jobs.Handlers;

internal sealed class GoalAutoContributionSyncHandler(
    IGoalService goalService) : JobHandlerBase<GoalAutoContributionSyncPayload>
{
    public override string JobType => JobTypes.GoalAutoContributionSync;

    protected override Task HandleAsync(GoalAutoContributionSyncPayload payload, CancellationToken ct) =>
        goalService.SyncAutoContributionsAsync(payload.ProcessingDate, ct);
}
