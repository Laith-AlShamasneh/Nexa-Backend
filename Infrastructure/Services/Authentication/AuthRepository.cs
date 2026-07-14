using Application.Features.Authentication.DbModels;
using Application.Interfaces.Database;
using Application.Interfaces.Repositories;
using Dapper;
using System.Data;

namespace Infrastructure.Services.Authentication;

internal sealed class AuthRepository(IDbExecutor db) : IAuthRepository
{
    public async Task<bool> CheckEmailExistsAsync(string email, CancellationToken ct = default)
    {
        var p = new DynamicParameters();
        p.Add("@Email", email, DbType.String);
        var result = await db.ExecuteScalarAsync<bool?>("MyMoney.usp_Authentication_CheckEmailExists", p, ct);
        return result ?? false;
    }

    public Task<Guid?> GetSecurityStampAsync(long userId, CancellationToken ct = default)
    {
        var p = new DynamicParameters();
        p.Add("@UserId", userId, DbType.Int64);
        return db.QuerySingleAsync<Guid?>("MyMoney.usp_Authentication_GetSecurityStamp", p, ct);
    }

    public Task BumpSecurityStampAsync(long userId, CancellationToken ct = default)
    {
        var p = new DynamicParameters();
        p.Add("@UserId", userId, DbType.Int64);
        return db.ExecuteAsync("MyMoney.usp_Authentication_BumpSecurityStamp", p, ct);
    }

    public async Task<RegisterDbResult?> RegisterAsync(RegisterDbInput input, CancellationToken ct = default)
    {
        var p = new DynamicParameters();
        p.Add("@FirstNameEn",    input.FirstNameEn,                                                    DbType.String);
        p.Add("@LastNameEn",     input.LastNameEn,                                                     DbType.String);
        p.Add("@FirstNameAr",    input.FirstNameAr,                                                    DbType.String);
        p.Add("@LastNameAr",     input.LastNameAr,                                                     DbType.String);
        p.Add("@DisplayNameEn",  input.DisplayNameEn,                                                  DbType.String);
        p.Add("@DisplayNameAr",  input.DisplayNameAr,                                                  DbType.String);
        p.Add("@DateOfBirth",    input.DateOfBirth?.ToDateTime(TimeOnly.MinValue),                     DbType.DateTime2);
        p.Add("@GenderId",       input.GenderId.HasValue ? (byte)input.GenderId.Value : (byte?)null,   DbType.Byte);
        p.Add("@ProfilePicture", input.ProfilePicture,                                                 DbType.String);
        p.Add("@Email",          input.Email,                                                          DbType.String);
        p.Add("@PasswordHash",   input.PasswordHash,                                                   DbType.String);
        p.Add("@DefaultRoleId",  input.DefaultRoleId,                                                  DbType.Int32);

        return await db.QuerySingleAsync<RegisterDbResult>("MyMoney.usp_Authentication_Register", p, ct);
    }

    public async Task SaveRefreshTokenAsync(SaveRefreshTokenDbInput input, CancellationToken ct = default)
    {
        var p = new DynamicParameters();
        p.Add("@UserId",       input.UserId,       DbType.Int64);
        p.Add("@Token",        input.Token,        DbType.String);
        p.Add("@ExpiresOnUtc", input.ExpiresOnUtc, DbType.DateTime2);
        p.Add("@CreatedByIp",  input.CreatedByIp,  DbType.String);

        await db.ExecuteAsync("MyMoney.usp_Authentication_SaveRefreshToken", p, ct);
    }

    public async Task<LoginDbResult?> GetByEmailForLoginAsync(string email, CancellationToken ct = default)
    {
        var p = new DynamicParameters();
        p.Add("@Email", email, DbType.String);

        return await db.QuerySingleAsync<LoginDbResult>("MyMoney.usp_Authentication_Login", p, ct);
    }

    public async Task UpdateLoginAsync(LoginUpdateDbModel model, CancellationToken ct = default)
    {
        var p = new DynamicParameters();
        p.Add("@UserId",                 model.UserId,                 DbType.Int64);
        p.Add("@LoginSucceeded",         model.LoginSucceeded,         DbType.Boolean);
        p.Add("@MaxFailedAttempts",      model.MaxFailedAttempts,      DbType.Int32);
        p.Add("@LockoutDurationMinutes", model.LockoutDurationMinutes, DbType.Int32);

        await db.ExecuteAsync("MyMoney.usp_Authentication_UpdateLogin", p, ct);
    }

    public async Task SaveConfirmationTokenAsync(SaveConfirmationTokenDbInput input, CancellationToken ct = default)
    {
        var p = new DynamicParameters();
        p.Add("@UserId",       input.UserId,       DbType.Int64);
        p.Add("@TokenHash",    input.TokenHash,    DbType.String);
        p.Add("@ExpiresAtUtc", input.ExpiresAtUtc, DbType.DateTime2);
        p.Add("@CreatedByIp",  input.CreatedByIp,  DbType.String);

        await db.ExecuteAsync("MyMoney.usp_Authentication_SaveConfirmationToken", p, ct);
    }

    public async Task<ConfirmEmailDbResult> ConfirmEmailAsync(ConfirmEmailDbInput input, CancellationToken ct = default)
    {
        var p = new DynamicParameters();
        p.Add("@TokenHash", input.TokenHash, DbType.String);
        p.Add("@UsedByIp",  input.UsedByIp,  DbType.String);

        return await db.QuerySingleAsync<ConfirmEmailDbResult>(
            "MyMoney.usp_Authentication_ConfirmEmail", p, ct)
            ?? new ConfirmEmailDbResult { ResultCode = 1 };
    }

    public async Task<UserConfirmationStatusDbResult?> GetUserConfirmationStatusAsync(
        string email, CancellationToken ct = default)
    {
        var p = new DynamicParameters();
        p.Add("@Email", email, DbType.String);

        return await db.QuerySingleAsync<UserConfirmationStatusDbResult>(
            "MyMoney.usp_Authentication_GetUserConfirmationStatus", p, ct);
    }

    public async Task<ChangePasswordUserDbResult?> GetUserForChangePasswordAsync(
        long userId, CancellationToken ct = default)
    {
        var p = new DynamicParameters();
        p.Add("@UserId", userId, DbType.Int64);

        return await db.QuerySingleAsync<ChangePasswordUserDbResult>(
            "MyMoney.usp_Authentication_GetUserForChangePassword", p, ct);
    }

    public async Task<ChangePasswordDbResult> ChangePasswordAsync(
        ChangePasswordDbInput input, CancellationToken ct = default)
    {
        var p = new DynamicParameters();
        p.Add("@UserId",            input.UserId,            DbType.Int64);
        p.Add("@NewPasswordHash",   input.NewPasswordHash,   DbType.String);
        p.Add("@ChangedByIp",       input.ChangedByIp,       DbType.String);
        p.Add("@CurrentTokenHash",  input.CurrentTokenHash,  DbType.String);

        return await db.QuerySingleAsync<ChangePasswordDbResult>(
            "MyMoney.usp_Authentication_ChangePassword", p, ct)
            ?? new ChangePasswordDbResult { ResultCode = 1 };
    }

    public async Task<ForgotPasswordDbResult?> GetUserForPasswordResetAsync(
        string email, CancellationToken ct = default)
    {
        var p = new DynamicParameters();
        p.Add("@Email", email, DbType.String);

        return await db.QuerySingleAsync<ForgotPasswordDbResult>(
            "MyMoney.usp_Authentication_GetUserForPasswordReset", p, ct);
    }

    public async Task SavePasswordResetTokenAsync(
        SavePasswordResetTokenDbInput input, CancellationToken ct = default)
    {
        var p = new DynamicParameters();
        p.Add("@UserId",       input.UserId,       DbType.Int64);
        p.Add("@TokenHash",    input.TokenHash,    DbType.String);
        p.Add("@ExpiresAtUtc", input.ExpiresAtUtc, DbType.DateTime2);
        p.Add("@CreatedByIp",  input.CreatedByIp,  DbType.String);

        await db.ExecuteAsync("MyMoney.usp_Authentication_SavePasswordResetToken", p, ct);
    }

    public async Task<ValidateResetTokenDbResult> ValidatePasswordResetTokenAsync(
        string tokenHash, CancellationToken ct = default)
    {
        var p = new DynamicParameters();
        p.Add("@TokenHash", tokenHash, DbType.String);

        return await db.QuerySingleAsync<ValidateResetTokenDbResult>(
            "MyMoney.usp_Authentication_ValidatePasswordResetToken", p, ct)
            ?? new ValidateResetTokenDbResult { ResultCode = 1 };
    }

    public async Task<ResetPasswordDbResult> ResetPasswordAsync(
        ResetPasswordDbInput input, CancellationToken ct = default)
    {
        var p = new DynamicParameters();
        p.Add("@TokenHash",    input.TokenHash,    DbType.String);
        p.Add("@PasswordHash", input.PasswordHash, DbType.String);
        p.Add("@UsedByIp",     input.UsedByIp,     DbType.String);

        return await db.QuerySingleAsync<ResetPasswordDbResult>(
            "MyMoney.usp_Authentication_ResetPassword", p, ct)
            ?? new ResetPasswordDbResult { ResultCode = 1 };
    }

    public async Task<RefreshTokenDbResult> RefreshTokenAsync(
        RefreshTokenDbInput input, CancellationToken ct = default)
    {
        var p = new DynamicParameters();
        p.Add("@OldTokenHash",    input.OldTokenHash,    DbType.String);
        p.Add("@NewTokenHash",    input.NewTokenHash,    DbType.String);
        p.Add("@NewExpiresOnUtc", input.NewExpiresOnUtc, DbType.DateTime2);
        p.Add("@RevokedByIp",     input.RevokedByIp,     DbType.String);

        return await db.QuerySingleAsync<RefreshTokenDbResult>(
            "MyMoney.usp_Authentication_RefreshToken", p, ct)
            ?? new RefreshTokenDbResult { ResultCode = 1 };
    }

    public async Task LogoutAsync(LogoutDbInput input, CancellationToken ct = default)
    {
        var p = new DynamicParameters();
        p.Add("@TokenHash",   input.TokenHash,   DbType.String);
        p.Add("@RevokedByIp", input.RevokedByIp, DbType.String);

        await db.ExecuteAsync("MyMoney.usp_Authentication_Logout", p, ct);
    }

    // ─── Email Change ──────────────────────────────────────────────────────────

    public async Task<GetProfileForEmailChangeDbResult?> GetProfileForEmailChangeAsync(
        long userId, CancellationToken ct = default)
    {
        var p = new DynamicParameters();
        p.Add("@UserId", userId, DbType.Int64);

        return await db.QuerySingleAsync<GetProfileForEmailChangeDbResult>(
            "MyMoney.usp_Profile_GetProfileForEmailChange", p, ct);
    }

    public async Task RequestEmailChangeAsync(
        RequestEmailChangeDbInput input, CancellationToken ct = default)
    {
        var p = new DynamicParameters();
        p.Add("@UserId",       input.UserId,       DbType.Int64);
        p.Add("@NewEmail",     input.NewEmail,     DbType.String);
        p.Add("@TokenHash",    input.TokenHash,    DbType.String);
        p.Add("@ExpiresAtUtc", input.ExpiresAtUtc, DbType.DateTime2);
        p.Add("@CreatedByIp",  input.CreatedByIp,  DbType.String);

        await db.ExecuteAsync("MyMoney.usp_Profile_RequestEmailChange", p, ct);
    }

    public async Task<ConfirmEmailChangeDbResult> ConfirmEmailChangeAsync(
        ConfirmEmailChangeDbInput input, CancellationToken ct = default)
    {
        var p = new DynamicParameters();
        p.Add("@TokenHash", input.TokenHash, DbType.String);
        p.Add("@UsedByIp",  input.UsedByIp,  DbType.String);

        return await db.QuerySingleAsync<ConfirmEmailChangeDbResult>(
            "MyMoney.usp_Profile_ConfirmEmailChange", p, ct)
            ?? new ConfirmEmailChangeDbResult(1, null, null, null, null, null);
    }

    public async Task CancelEmailChangeAsync(long userId, CancellationToken ct = default)
    {
        var p = new DynamicParameters();
        p.Add("@UserId", userId, DbType.Int64);

        await db.ExecuteAsync("MyMoney.usp_Profile_CancelEmailChange", p, ct);
    }
}
