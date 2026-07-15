using System.Data;
using Application.Features.ScheduledJobs.DbModels;
using Application.Interfaces.Repositories;
using Dapper;
using Infrastructure.Database;

namespace Infrastructure.Jobs;

internal sealed class ScheduledJobRepository(IDbExecutor db) : IScheduledJobRepository
{
    public async Task<long> CreateAsync(ScheduledJobCreateInput input, CancellationToken ct = default)
    {
        var p = new DynamicParameters();
        p.Add("@Name",            input.Name,            DbType.String, size: 200);
        p.Add("@Description",     input.Description,     DbType.String, size: 500);
        p.Add("@JobType",         input.JobType,          DbType.String, size: 200);
        p.Add("@PayloadTemplate", input.PayloadTemplate, DbType.String);
        p.Add("@CronExpression",  input.CronExpression,  DbType.String, size: 100);
        p.Add("@IntervalSeconds", input.IntervalSeconds, DbType.Int32);
        p.Add("@Priority",        input.Priority,        DbType.Byte);
        p.Add("@MaxAttempts",     input.MaxAttempts,     DbType.Int32);
        p.Add("@NextRunAt",       input.NextRunAt,       DbType.DateTime2);
        p.Add("@OrganizationId",  input.OrganizationId,  DbType.Guid);
        p.Add("@CreatedBy",       input.CreatedBy,       DbType.Guid);

        return await db.ExecuteScalarAsync<long>("dbo.usp_ScheduledJob_Create", p, ct);
    }

    public Task<IReadOnlyList<ScheduledJobRow>> ClaimDueAsync(int batchSize, int claimTimeoutSeconds, CancellationToken ct = default)
    {
        var p = new DynamicParameters();
        p.Add("@BatchSize",           batchSize,           DbType.Int32);
        p.Add("@ClaimTimeoutSeconds", claimTimeoutSeconds, DbType.Int32);
        return db.QueryListAsync<ScheduledJobRow>("dbo.usp_ScheduledJob_ClaimDue", p, ct);
    }

    public Task CompleteRunAsync(ScheduledJobCompleteRunInput input, CancellationToken ct = default)
    {
        var p = new DynamicParameters();
        p.Add("@ScheduledJobId",    input.ScheduledJobId,    DbType.Int64);
        p.Add("@NextRunAt",         input.NextRunAt,         DbType.DateTime2);
        p.Add("@LastEnqueuedJobId", input.LastEnqueuedJobId, DbType.Int64);
        return db.ExecuteAsync("dbo.usp_ScheduledJob_CompleteRun", p, ct);
    }

    public Task SetEnabledAsync(long scheduledJobId, bool isEnabled, Guid? updatedBy, CancellationToken ct = default)
    {
        var p = new DynamicParameters();
        p.Add("@ScheduledJobId", scheduledJobId, DbType.Int64);
        p.Add("@IsEnabled",      isEnabled,      DbType.Boolean);
        p.Add("@UpdatedBy",      updatedBy,      DbType.Guid);
        return db.ExecuteAsync("dbo.usp_ScheduledJob_SetEnabled", p, ct);
    }
}
