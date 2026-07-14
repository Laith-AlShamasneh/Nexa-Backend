namespace Application.Features.Authentication.DbModels;

public sealed class SaveConfirmationTokenDbInput
{
    public long     UserId       { get; init; }
    public string   TokenHash    { get; init; } = null!;
    public DateTime ExpiresAtUtc { get; init; }
    public string?  CreatedByIp  { get; init; }
}

public sealed class ConfirmEmailDbInput
{
    public string  TokenHash { get; init; } = null!;
    public string? UsedByIp  { get; init; }
}

public sealed class ConfirmEmailDbResult
{
    /// <summary>
    /// 0 = Success, 1 = NotFound, 2 = Expired, 3 = AlreadyUsed, 4 = AlreadyConfirmed
    /// </summary>
    public byte ResultCode { get; init; }
}

public sealed class UserConfirmationStatusDbResult
{
    public long    UserId           { get; init; }
    public bool    IsEmailConfirmed { get; init; }
    public bool    IsActive         { get; init; }
    public string  DisplayNameEn    { get; init; } = null!;
    public string? DisplayNameAr    { get; init; }
}
