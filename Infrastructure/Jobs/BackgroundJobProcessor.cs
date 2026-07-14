using Application.Features.BackgroundJobs.DbModels;
using Application.Interfaces.Jobs;
using Application.Interfaces.Repositories;
using Infrastructure.Jobs.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure.Jobs;

internal sealed class BackgroundJobProcessor(
    IServiceProvider              serviceProvider,
    IOptions<BackgroundJobOptions> options,
    ILogger<BackgroundJobProcessor> logger) : BackgroundService
{
    private readonly BackgroundJobOptions _options = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessBatchAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Unhandled exception in BackgroundJobProcessor polling loop.");
            }

            await Task.Delay(_options.PollingIntervalSeconds * 1_000, stoppingToken);
        }
    }

    private async Task ProcessBatchAsync(CancellationToken ct)
    {
        IReadOnlyList<BackgroundJobRow> jobs;

        // Pick up jobs in a dedicated scope so the repository gets its own IDbExecutor.
        await using (var pickupScope = serviceProvider.CreateAsyncScope())
        {
            var repository = pickupScope.ServiceProvider.GetRequiredService<IBackgroundJobRepository>();
            jobs = await repository.PickUpPendingJobsAsync(_options.BatchSize, ct);
        }

        if (jobs.Count == 0) return;

        // Process the batch with bounded parallelism — each job gets its own scope/connection.
        await Parallel.ForEachAsync(
            jobs,
            new ParallelOptions
            {
                MaxDegreeOfParallelism = Math.Max(1, _options.MaxDegreeOfParallelism),
                CancellationToken      = ct
            },
            async (job, token) => await ExecuteJobAsync(job, token));
    }

    private async Task ExecuteJobAsync(BackgroundJobRow job, CancellationToken ct)
    {
        await using var scope = serviceProvider.CreateAsyncScope();
        var repository = scope.ServiceProvider.GetRequiredService<IBackgroundJobRepository>();

        // Cap each job's runtime so one hung handler cannot stall the batch.
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeoutCts.CancelAfter(TimeSpan.FromSeconds(_options.JobTimeoutSeconds));

        try
        {
            var handlers = scope.ServiceProvider.GetServices<IJobHandler>();
            var handler  = handlers.FirstOrDefault(h => h.JobType == job.JobType);

            if (handler is null)
            {
                logger.LogWarning("No handler registered for job type '{JobType}' (JobId={JobId}).", job.JobType, job.JobId);
                await repository.FailAsync(new BackgroundJobFailInput
                {
                    JobId        = job.JobId,
                    ErrorMessage = $"No handler registered for job type '{job.JobType}'.",
                    AttemptCount = job.AttemptCount,
                    MaxAttempts  = job.MaxAttempts
                }, ct);
                return;
            }

            await handler.HandleAsync(job.Payload, timeoutCts.Token);
            await repository.CompleteAsync(job.JobId, ct);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            // App shutdown: leave the job in its current state for re-pickup; don't mark failed.
            throw;
        }
        catch (Exception ex)
        {
            var timedOut = timeoutCts.IsCancellationRequested && !ct.IsCancellationRequested;
            var errorMessage = timedOut
                ? $"Job timed out after {_options.JobTimeoutSeconds}s."
                : ex.Message;

            logger.LogError(ex, "Job {JobId} ({JobType}) failed on attempt {Attempt}/{Max}.{TimedOut}",
                job.JobId, job.JobType, job.AttemptCount, job.MaxAttempts, timedOut ? " (timed out)" : string.Empty);

            await repository.FailAsync(new BackgroundJobFailInput
            {
                JobId        = job.JobId,
                ErrorMessage = errorMessage,
                AttemptCount = job.AttemptCount,
                MaxAttempts  = job.MaxAttempts
            }, ct);
        }
    }
}
