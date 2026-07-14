using Application.Features.Onboarding.DbModels;
using Application.Interfaces.Database;
using Application.Interfaces.Repositories;
using Dapper;
using System.Data;

namespace Infrastructure.Services.Onboarding;

internal sealed class OnboardingRepository(IDbExecutor db) : IOnboardingRepository
{
    public Task InitializeAsync(long userId, CancellationToken ct = default)
    {
        var p = new DynamicParameters();
        p.Add("@UserId", userId, DbType.Int64);
        return db.ExecuteAsync("MyMoney.usp_Onboarding_Initialize", p, ct);
    }

    public Task<IReadOnlyList<OnboardingStateDbResult>> GetStateAsync(long userId, CancellationToken ct = default)
    {
        var p = new DynamicParameters();
        p.Add("@UserId", userId, DbType.Int64);
        return db.QueryListAsync<OnboardingStateDbResult>("MyMoney.usp_Onboarding_GetState", p, ct);
    }

    public async Task<AdvanceStepDbResult> AdvanceStepAsync(AdvanceStepDbInput input, CancellationToken ct = default)
    {
        var p = new DynamicParameters();
        p.Add("@UserId",     input.UserId,     DbType.Int64);
        p.Add("@StepKey",    input.StepKey,    DbType.String);
        p.Add("@StepStatus", input.StepStatus, DbType.Byte);

        return await db.QuerySingleAsync<AdvanceStepDbResult>(
            "MyMoney.usp_Onboarding_AdvanceStep", p, ct)
            ?? new AdvanceStepDbResult { ResultCode = 1 };
    }

    public async Task<SkipOnboardingDbResult> SkipAsync(long userId, CancellationToken ct = default)
    {
        var p = new DynamicParameters();
        p.Add("@UserId", userId, DbType.Int64);

        return await db.QuerySingleAsync<SkipOnboardingDbResult>(
            "MyMoney.usp_Onboarding_Skip", p, ct)
            ?? new SkipOnboardingDbResult { ResultCode = 1 };
    }
}
