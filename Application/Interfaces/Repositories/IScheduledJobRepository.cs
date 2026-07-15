using Application.Features.ScheduledJobs.DbModels;

namespace Application.Interfaces.Repositories;

public interface IScheduledJobRepository
{
    Task<long> CreateAsync(ScheduledJobCreateInput input, CancellationToken ct = default);

    /// <summary>
    /// Atomically claims up to <paramref name="batchSize"/> due, enabled
    /// schedules. A claim older than <paramref name="claimTimeoutSeconds"/>
    /// is treated as abandoned (owning instance crashed mid-run) and is
    /// reclaimable.
    /// </summary>
    Task<IReadOnlyList<ScheduledJobRow>> ClaimDueAsync(int batchSize, int claimTimeoutSeconds, CancellationToken ct = default);

    /// <summary>Releases a claim, advances NextRunAt, and records what got enqueued.</summary>
    Task CompleteRunAsync(ScheduledJobCompleteRunInput input, CancellationToken ct = default);

    Task SetEnabledAsync(long scheduledJobId, bool isEnabled, Guid? updatedBy, CancellationToken ct = default);
}
