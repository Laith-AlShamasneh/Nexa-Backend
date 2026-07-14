using Application.Common.Constants;
using Application.Features.FinancialIntelligence;
using Application.Features.FinancialIntelligence.Jobs;
using Infrastructure.Jobs;

namespace Infrastructure.Jobs.Handlers;

internal sealed class SnapshotRecomputeHandler(
    IFILBackgroundProcessingService filService) : JobHandlerBase<SnapshotRecomputePayload>
{
    public override string JobType => JobTypes.SnapshotRecompute;

    protected override Task HandleAsync(SnapshotRecomputePayload payload, CancellationToken ct) =>
        filService.ProcessUserSnapshotAsync(payload.UserId, payload.WorkspaceId, payload.Year, payload.Month, ct);
}
