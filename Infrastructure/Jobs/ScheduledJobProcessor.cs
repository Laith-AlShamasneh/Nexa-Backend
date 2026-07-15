using Application.Features.BackgroundJobs.DbModels;
using Application.Features.ScheduledJobs.DbModels;
using Application.Interfaces.Repositories;
using Cronos;
using Infrastructure.Jobs.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure.Jobs;

/// <summary>
/// Polls dbo.ScheduledJobs for due, enabled recurring-job definitions and
/// turns each one into a dbo.BackgroundJobs row. Only registered when
/// BackgroundJobOptions.RunSchedulers is true (see ServiceCollectionExtensions).
///
/// Claiming is atomic (UPDLOCK/READPAST in usp_ScheduledJob_ClaimDue), so
/// running this on multiple instances is safe — each due schedule is only
/// claimed by one instance at a time. This is the gap the old (MyMoney)
/// hardcoded-timer approach had: with more than one instance, every timer
/// fired independently and the same recurring action ran multiple times.
/// </summary>
internal sealed class ScheduledJobProcessor(
    IServiceProvider               serviceProvider,
    IOptions<BackgroundJobOptions> options,
    ILogger<ScheduledJobProcessor> logger) : BackgroundService
{
    private const int ClaimTimeoutSeconds = 300;

    private readonly BackgroundJobOptions _options = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessDueAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Unhandled exception in ScheduledJobProcessor polling loop.");
            }

            await Task.Delay(_options.PollingIntervalSeconds * 1_000, stoppingToken);
        }
    }

    private async Task ProcessDueAsync(CancellationToken ct)
    {
        IReadOnlyList<ScheduledJobRow> due;

        await using (var claimScope = serviceProvider.CreateAsyncScope())
        {
            var repository = claimScope.ServiceProvider.GetRequiredService<IScheduledJobRepository>();
            due = await repository.ClaimDueAsync(_options.BatchSize, ClaimTimeoutSeconds, ct);
        }

        if (due.Count == 0) return;

        // Sequential: schedule triggers are low-volume and each is cheap (one
        // enqueue + one update) — parallelism here would add complexity the
        // workload doesn't need. BackgroundJobProcessor parallelizes because
        // job handlers can do real, slow work; triggering a schedule never does.
        foreach (var schedule in due)
            await TriggerAsync(schedule, ct);
    }

    private async Task TriggerAsync(ScheduledJobRow schedule, CancellationToken ct)
    {
        await using var scope = serviceProvider.CreateAsyncScope();
        var backgroundJobRepository = scope.ServiceProvider.GetRequiredService<IBackgroundJobRepository>();
        var scheduledJobRepository  = scope.ServiceProvider.GetRequiredService<IScheduledJobRepository>();

        try
        {
            var nextRunAt = ComputeNextRunAt(schedule);

            var jobId = await backgroundJobRepository.EnqueueAsync(new BackgroundJobEnqueueInput
            {
                JobType        = schedule.JobType,
                Payload        = schedule.PayloadTemplate ?? "{}",
                Priority       = schedule.Priority,
                MaxAttempts    = schedule.MaxAttempts,
                OrganizationId = schedule.OrganizationId
            }, ct);

            await scheduledJobRepository.CompleteRunAsync(new ScheduledJobCompleteRunInput
            {
                ScheduledJobId    = schedule.ScheduledJobId,
                NextRunAt         = nextRunAt,
                LastEnqueuedJobId = jobId
            }, ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Deliberately don't call CompleteRunAsync here — leaving the claim in
            // place lets usp_ScheduledJob_ClaimDue's timeout window reclaim it on a
            // later poll, the same "retry after abandonment" safety net PickUp uses
            // for BackgroundJobs.
            logger.LogError(ex, "Failed to trigger scheduled job {ScheduledJobId} ({JobType}).",
                schedule.ScheduledJobId, schedule.JobType);
        }
    }

    private static DateTime ComputeNextRunAt(ScheduledJobRow schedule)
    {
        if (schedule.IntervalSeconds is int seconds)
            return DateTime.UtcNow.AddSeconds(seconds);

        // CK_ScheduledJobs_ScheduleSource guarantees exactly one of
        // CronExpression/IntervalSeconds is set, so this is never null here.
        var cron = CronExpression.Parse(schedule.CronExpression!, CronFormat.Standard);
        return cron.GetNextOccurrence(DateTime.UtcNow, TimeZoneInfo.Utc)
            ?? DateTime.UtcNow.AddMinutes(1);
    }
}
