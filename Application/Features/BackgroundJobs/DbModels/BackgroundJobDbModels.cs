namespace Application.Features.BackgroundJobs.DbModels;

public sealed class BackgroundJobEnqueueInput
{
    public string    JobType     { get; init; } = string.Empty;
    public string    Payload     { get; init; } = string.Empty;
    public byte      Priority    { get; init; } = 2;
    public DateTime  ScheduledAt { get; init; } = DateTime.UtcNow;
    public int       MaxAttempts { get; init; } = 3;

    // Guid, matching identity.Users.Id — NOT IUserContext.UserId, which is still
    // long pending the documented long/Guid identity reconciliation (see
    // docs/BACKGROUND_JOBS.md). Callers that only have the long user id must
    // leave this null until that reconciliation lands.
    public Guid?     CreatedBy   { get; init; }

    // Informational only — most jobs still carry their tenant context inside
    // Payload; this exists so tooling can filter/cancel by tenant without
    // parsing JSON. Null for platform-level jobs.
    public Guid?     OrganizationId { get; init; }

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

/// <summary>Maps the single-row result set from dbo.usp_BackgroundJob_Enqueue.</summary>
public sealed class BackgroundJobEnqueueResult
{
    public int   ResultCode { get; init; }
    public long? JobId      { get; init; }
}
