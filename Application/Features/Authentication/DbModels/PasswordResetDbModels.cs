namespace Application.Features.Authentication.DbModels;

public sealed class ForgotPasswordDbResult
{
    public long    UserId        { get; init; }
    public bool    IsActive      { get; init; }
    public string  DisplayNameEn { get; init; } = null!;
    public string? DisplayNameAr { get; init; }
}

public sealed class SavePasswordResetTokenDbInput
{
    public long     UserId       { get; init; }
    public string   TokenHash    { get; init; } = null!;
    public DateTime ExpiresAtUtc { get; init; }
    public string?  CreatedByIp  { get; init; }
}

public sealed class ValidateResetTokenDbResult
{
    /// <summary>
    /// 0 = Valid, 1 = NotFound, 2 = Expired, 3 = AlreadyUsed, 4 = UserInactive
    /// </summary>
    public byte ResultCode { get; init; }
}

public sealed class ResetPasswordDbInput
{
    public string  TokenHash    { get; init; } = null!;
    public string  PasswordHash { get; init; } = null!;
    public string? UsedByIp     { get; init; }
}

public sealed class ResetPasswordDbResult
{
    /// <summary>
    /// 0 = Success, 1 = NotFound, 2 = Expired, 3 = AlreadyUsed, 4 = UserInactive
    /// </summary>
    public byte ResultCode { get; init; }
}
