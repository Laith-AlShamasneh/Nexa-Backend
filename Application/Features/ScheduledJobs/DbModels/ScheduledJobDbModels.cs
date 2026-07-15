namespace Application.Features.ScheduledJobs.DbModels;

public sealed class ScheduledJobCreateInput
{
    public string    Name            { get; init; } = string.Empty;
    public string?   Description     { get; init; }
    public string    JobType         { get; init; } = string.Empty;
    public string?   PayloadTemplate { get; init; }

    // Exactly one of CronExpression / IntervalSeconds must be set — enforced
    // by CK_ScheduledJobs_ScheduleSource at the database level.
    public string?   CronExpression  { get; init; }
    public int?      IntervalSeconds { get; init; }

    public byte      Priority        { get; init; } = 2;
    public int       MaxAttempts     { get; init; } = 3;
    public DateTime  NextRunAt       { get; init; }
    public Guid?     OrganizationId  { get; init; }
    public Guid?     CreatedBy       { get; init; }
}

/// <summary>A schedule claimed via usp_ScheduledJob_ClaimDue, ready to be triggered.</summary>
public sealed class ScheduledJobRow
{
    public long     ScheduledJobId  { get; init; }
    public Guid?    OrganizationId  { get; init; }
    public string   JobType         { get; init; } = string.Empty;
    public string?  PayloadTemplate { get; init; }
    public string?  CronExpression  { get; init; }
    public int?     IntervalSeconds { get; init; }
    public byte     Priority        { get; init; }
    public int      MaxAttempts     { get; init; }
    public DateTime NextRunAt       { get; init; }
}

public sealed class ScheduledJobCompleteRunInput
{
    public long     ScheduledJobId    { get; init; }
    public DateTime NextRunAt         { get; init; }
    public long?    LastEnqueuedJobId { get; init; }
}
