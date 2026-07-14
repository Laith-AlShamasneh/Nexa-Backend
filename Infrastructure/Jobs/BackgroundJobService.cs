using System.Text.Json;
using Application.Features.BackgroundJobs.DbModels;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;

namespace Infrastructure.Jobs;

internal sealed class BackgroundJobService(
    IBackgroundJobRepository repository,
    IUserContext             userContext) : IBackgroundJobService
{
    public Task EnqueueAsync<TPayload>(
        string    jobType,
        TPayload  payload,
        byte      priority    = 2,
        DateTime? scheduledAt = null,
        int       maxAttempts = 3,
        string?   dedupKey    = null,
        CancellationToken ct  = default)
    {
        var input = new BackgroundJobEnqueueInput
        {
            JobType     = jobType,
            Payload     = JsonSerializer.Serialize(payload),
            Priority    = priority,
            ScheduledAt = scheduledAt ?? DateTime.UtcNow,
            MaxAttempts = maxAttempts,
            CreatedBy   = userContext.IsAuthenticated ? userContext.UserId : null,
            DedupKey    = dedupKey
        };
        return repository.EnqueueAsync(input, ct);
    }
}
