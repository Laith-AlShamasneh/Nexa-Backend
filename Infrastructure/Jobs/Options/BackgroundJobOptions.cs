namespace Infrastructure.Jobs.Options;

public sealed class BackgroundJobOptions
{
    public int PollingIntervalSeconds  { get; init; } = 10;
    public int BatchSize               { get; init; } = 20;

    // How many jobs from a picked-up batch run concurrently. Each job uses its own
    // DI scope and DB connection, so concurrency is safe.
    public int MaxDegreeOfParallelism  { get; init; } = 4;

    // A single hung handler must not stall the batch indefinitely.
    public int JobTimeoutSeconds       { get; init; } = 300;

    // Whether this instance runs the timer-based schedulers (see InfrastructureRegistration).
    public bool RunSchedulers          { get; init; } = true;
}
