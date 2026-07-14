using System.Data;
using Application.Features.Tenancy.DbModels;
using Application.Interfaces.Repositories;
using Dapper;
using Infrastructure.Database;

namespace Infrastructure.Services.Tenancy;

internal sealed class OrganizationRegistrationRepository(IDbExecutor db) : IOrganizationRegistrationRepository
{
    public async Task<RegisterOrganizationDbResult> RegisterAsync(
        RegisterOrganizationDbInput input, CancellationToken ct = default)
    {
        var p = new DynamicParameters();
        p.Add("@OrganizationId",              input.OrganizationId,              DbType.Guid);
        p.Add("@OrganizationName",            input.OrganizationName,            DbType.String);
        p.Add("@OrganizationArabicName",      input.OrganizationArabicName,      DbType.String);
        p.Add("@OrganizationLegalName",       input.OrganizationLegalName,       DbType.String);
        p.Add("@OrganizationArabicLegalName", input.OrganizationArabicLegalName, DbType.String);
        p.Add("@Slug",                        input.Slug,                        DbType.String);
        p.Add("@LogoUrl",                     input.LogoUrl,                     DbType.String);
        p.Add("@OrganizationEmail",           input.OrganizationEmail,           DbType.String);
        p.Add("@OrganizationPhone",           input.OrganizationPhone,           DbType.String);
        p.Add("@OrganizationAddress",         input.OrganizationAddress,         DbType.String);

        p.Add("@TimeZoneId",          input.TimeZoneId,          DbType.String);
        p.Add("@DefaultLanguageCode", input.DefaultLanguageCode, DbType.String);
        p.Add("@CurrencyCode",        input.CurrencyCode,        DbType.AnsiStringFixedLength, size: 3);

        p.Add("@BranchId",         input.BranchId,         DbType.Guid);
        p.Add("@BranchName",       input.BranchName,       DbType.String);
        p.Add("@BranchArabicName", input.BranchArabicName, DbType.String);
        p.Add("@BranchPhone",      input.BranchPhone,       DbType.String);
        p.Add("@BranchEmail",      input.BranchEmail,       DbType.String);
        p.Add("@BranchAddress",    input.BranchAddress,     DbType.String);

        p.Add("@PersonId",        input.PersonId,        DbType.Guid);
        p.Add("@FirstName",       input.FirstName,       DbType.String);
        p.Add("@LastName",        input.LastName,        DbType.String);
        p.Add("@ArabicFirstName", input.ArabicFirstName, DbType.String);
        p.Add("@ArabicLastName",  input.ArabicLastName,  DbType.String);
        p.Add("@OwnerPhone",      input.OwnerPhone,       DbType.String);

        p.Add("@UserId",       input.UserId,       DbType.Guid);
        p.Add("@Username",     input.Username,     DbType.String);
        p.Add("@Email",        input.Email,        DbType.String);
        p.Add("@PasswordHash", input.PasswordHash, DbType.String);

        p.Add("@EmailConfirmationTokenHash",    input.EmailConfirmationTokenHash,    DbType.AnsiStringFixedLength, size: 64);
        p.Add("@EmailConfirmationExpiresAtUtc", input.EmailConfirmationExpiresAtUtc, DbType.DateTime2);

        p.Add("@CreatedByIp",   input.CreatedByIp,   DbType.String);
        p.Add("@CorrelationId", input.CorrelationId, DbType.Guid);

        var result = await db.QuerySingleAsync<RegisterOrganizationDbResult>(
            "tenant.usp_Organization_Register", p, ct);

        return result ?? throw new InvalidOperationException(
            "tenant.usp_Organization_Register returned no result set.");
    }
}
