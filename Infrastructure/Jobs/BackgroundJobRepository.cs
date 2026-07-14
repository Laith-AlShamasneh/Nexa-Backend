using System.Data;
using Application.Features.BackgroundJobs.DbModels;
using Application.Interfaces.Database;
using Application.Interfaces.Repositories;
using Dapper;

namespace Infrastructure.Jobs;

internal sealed class BackgroundJobRepository(IDbExecutor db) : IBackgroundJobRepository
{
    public Task EnqueueAsync(BackgroundJobEnqueueInput input, CancellationToken ct = default)
    {
        var p = new DynamicParameters();
        p.Add("@JobType",     input.JobType,     DbType.String,   size: 200);
        p.Add("@Payload",     input.Payload,     DbType.String);
        p.Add("@Priority",    input.Priority,    DbType.Byte);
        p.Add("@ScheduledAt", input.ScheduledAt, DbType.DateTime2);
        p.Add("@MaxAttempts", input.MaxAttempts, DbType.Int32);
        p.Add("@CreatedBy",   input.CreatedBy,   DbType.Int64);
        // Only sent when present so the call still works against an SP build that
        // predates the @DedupKey parameter (H7 migration applied separately).
        if (input.DedupKey is not null)
            p.Add("@DedupKey", input.DedupKey, DbType.String, size: 200);
        return db.ExecuteAsync("MyMoney.usp_BackgroundJob_Enqueue", p, ct);
    }

    public Task<IReadOnlyList<BackgroundJobRow>> PickUpPendingJobsAsync(int batchSize, CancellationToken ct = default)
    {
        var p = new DynamicParameters();
        p.Add("@BatchSize", batchSize, DbType.Int32);
        return db.QueryListAsync<BackgroundJobRow>("MyMoney.usp_BackgroundJob_PickUp", p, ct);
    }

    public Task CompleteAsync(long jobId, CancellationToken ct = default)
    {
        var p = new DynamicParameters();
        p.Add("@JobId", jobId, DbType.Int64);
        return db.ExecuteAsync("MyMoney.usp_BackgroundJob_Complete", p, ct);
    }

    public Task FailAsync(BackgroundJobFailInput input, CancellationToken ct = default)
    {
        var p = new DynamicParameters();
        p.Add("@JobId",        input.JobId,        DbType.Int64);
        p.Add("@ErrorMessage", input.ErrorMessage, DbType.String);
        p.Add("@AttemptCount", input.AttemptCount, DbType.Int32);
        p.Add("@MaxAttempts",  input.MaxAttempts,  DbType.Int32);
        return db.ExecuteAsync("MyMoney.usp_BackgroundJob_Fail", p, ct);
    }
}
