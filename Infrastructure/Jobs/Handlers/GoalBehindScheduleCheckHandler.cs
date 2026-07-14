using Application.Common.Constants;
using Application.Features.Goals;
using Application.Features.Goals.Jobs;
using Infrastructure.Jobs;

namespace Infrastructure.Jobs.Handlers;

internal sealed class GoalBehindScheduleCheckHandler(
    IGoalService goalService) : JobHandlerBase<GoalBehindScheduleCheckPayload>
{
    public override string JobType => JobTypes.GoalBehindScheduleCheck;

    protected override Task HandleAsync(GoalBehindScheduleCheckPayload payload, CancellationToken ct) =>
        goalService.CheckBehindScheduleAsync(ct);
}
