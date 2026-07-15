using Application.Features.BackgroundJobs.DbModels;

namespace Application.Interfaces.Repositories;

public interface IBackgroundJobRepository
{
    /// <summary>
    /// Returns the enqueued (or, on a dedup match, the pre-existing) JobId.
    /// Null only in the theoretical race where a dedup-matched job completed
    /// between the insert failing and the fallback lookup running.
    /// </summary>
    Task<long?> EnqueueAsync(BackgroundJobEnqueueInput input, CancellationToken ct = default);
    Task<IReadOnlyList<BackgroundJobRow>> PickUpPendingJobsAsync(int batchSize, CancellationToken ct = default);
    Task CompleteAsync(long jobId, CancellationToken ct = default);
    Task FailAsync(BackgroundJobFailInput input, CancellationToken ct = default);
}
