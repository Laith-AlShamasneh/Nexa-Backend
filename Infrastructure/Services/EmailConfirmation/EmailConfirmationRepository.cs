using System.Data;
using Application.Features.EmailConfirmation.DbModels;
using Application.Interfaces.Repositories;
using Dapper;
using Infrastructure.Database;

namespace Infrastructure.Services.EmailConfirmation;

internal sealed class EmailConfirmationRepository(IDbExecutor db) : IEmailConfirmationRepository
{
    public async Task<ConfirmEmailDbResult> ConfirmAsync(ConfirmEmailDbInput input, CancellationToken ct = default)
    {
        var p = new DynamicParameters();
        p.Add("@TokenHash",     input.TokenHash,     DbType.String, size: 64);
        p.Add("@UsedByIp",      input.UsedByIp,      DbType.String, size: 45);
        p.Add("@CorrelationId", input.CorrelationId, DbType.Guid);

        return await db.QuerySingleAsync<ConfirmEmailDbResult>("identity.usp_EmailConfirmation_Confirm", p, ct)
            ?? new ConfirmEmailDbResult { ResultCode = 2 };
    }

    public async Task<ResendEmailConfirmationDbResult> ResendAsync(ResendEmailConfirmationDbInput input, CancellationToken ct = default)
    {
        var p = new DynamicParameters();
        p.Add("@Email",                 input.Email,                 DbType.String, size: 256);
        p.Add("@NewTokenHash",          input.NewTokenHash,          DbType.String, size: 64);
        p.Add("@NewTokenExpiresAtUtc",  input.NewTokenExpiresAtUtc,  DbType.DateTime2);
        p.Add("@ResendCooldownSeconds", input.ResendCooldownSeconds, DbType.Int32);
        p.Add("@MaxResendsPerHour",     input.MaxResendsPerHour,     DbType.Int32);
        p.Add("@RequestIp",             input.RequestIp,             DbType.String, size: 45);
        p.Add("@CorrelationId",         input.CorrelationId,         DbType.Guid);

        return await db.QuerySingleAsync<ResendEmailConfirmationDbResult>("identity.usp_EmailConfirmation_Resend", p, ct)
            ?? new ResendEmailConfirmationDbResult { ResultCode = 1 };
    }
}
