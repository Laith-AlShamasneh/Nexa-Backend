namespace Application.Features.BackgroundJobs.DbModels;

public sealed class BackgroundJobEnqueueInput
{
    public string    JobType     { get; init; } = string.Empty;
    public string    Payload     { get; init; } = string.Empty;
    public byte      Priority    { get; init; } = 2;
    public DateTime  ScheduledAt { get; init; } = DateTime.UtcNow;
    public int       MaxAttempts { get; init; } = 3;
    public long?     CreatedBy   { get; init; }

    // Optional idempotency key. When set, the enqueue SP skips insertion if a
    // non-terminal (Pending/Processing) job with the same key already exists.
    public string?   DedupKey    { get; init; }
}

public sealed class BackgroundJobRow
{
    public long   JobId   { get; init; }
    public string JobType { get; init; } = string.Empty;
    public string Payload { get; init; } = string.Empty;
    public int    AttemptCount { get; init; }
    public int    MaxAttempts  { get; init; }
}

public sealed class BackgroundJobFailInput
{
    public long    JobId        { get; init; }
    public string  ErrorMessage { get; init; } = string.Empty;
    public int     AttemptCount { get; init; }
    public int     MaxAttempts  { get; init; }
}
