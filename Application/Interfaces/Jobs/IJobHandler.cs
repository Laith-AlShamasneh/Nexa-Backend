namespace Application.Interfaces.Jobs;

/// <summary>
/// Non-generic base: receives the raw JSON payload from the job table.
/// </summary>
public interface IJobHandler
{
    string JobType { get; }
    Task HandleAsync(string jsonPayload, CancellationToken ct);
}

/// <summary>
/// Strongly-typed marker for job handlers. Concrete handlers implement this interface.
/// </summary>
public interface IJobHandler<TPayload> : IJobHandler { }
