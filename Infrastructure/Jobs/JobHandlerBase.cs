using System.Text.Json;
using Application.Interfaces.Jobs;

namespace Infrastructure.Jobs;

/// <summary>
/// Abstract base for all job handlers. Deserializes the raw JSON payload
/// and delegates to the strongly-typed HandleAsync implementation.
/// </summary>
public abstract class JobHandlerBase<TPayload> : IJobHandler<TPayload>
{
    public abstract string JobType { get; }

    public Task HandleAsync(string jsonPayload, CancellationToken ct)
    {
        var payload = JsonSerializer.Deserialize<TPayload>(jsonPayload)
            ?? throw new InvalidOperationException($"Failed to deserialize payload for job type '{JobType}'.");
        return HandleAsync(payload, ct);
    }

    protected abstract Task HandleAsync(TPayload payload, CancellationToken ct);
}
