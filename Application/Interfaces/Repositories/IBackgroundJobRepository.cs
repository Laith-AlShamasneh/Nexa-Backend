using Application.Features.BackgroundJobs.DbModels;

namespace Application.Interfaces.Repositories;

public interface IBackgroundJobRepository
{
    Task EnqueueAsync(BackgroundJobEnqueueInput input, CancellationToken ct = default);
    Task<IReadOnlyList<BackgroundJobRow>> PickUpPendingJobsAsync(int batchSize, CancellationToken ct = default);
    Task CompleteAsync(long jobId, CancellationToken ct = default);
    Task FailAsync(BackgroundJobFailInput input, CancellationToken ct = default);
}
