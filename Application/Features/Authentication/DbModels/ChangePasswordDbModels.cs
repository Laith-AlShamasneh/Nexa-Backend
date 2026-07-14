namespace Application.Features.Authentication.DbModels;

public sealed class ChangePasswordUserDbResult
{
    public long    UserId        { get; init; }
    public string  PasswordHash  { get; init; } = null!;
    public bool    IsActive      { get; init; }
    public string  DisplayNameEn { get; init; } = null!;
    public string? DisplayNameAr { get; init; }
}

public sealed class ChangePasswordDbInput
{
    public long    UserId             { get; init; }
    public string  NewPasswordHash    { get; init; } = null!;
    public string? ChangedByIp        { get; init; }
    public string? CurrentTokenHash   { get; init; }
}

public sealed class ChangePasswordDbResult
{
    /// <summary>
    /// 0 = Success, 1 = UserNotFoundOrInactive
    /// </summary>
    public byte ResultCode { get; init; }
}
