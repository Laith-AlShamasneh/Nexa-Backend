namespace Application.Features.Authentication.DbModels;

public sealed record GetProfileForEmailChangeDbResult(
    string PasswordHash,
    bool   IsActive,
    string DisplayNameEn,
    string? DisplayNameAr,
    string Email
);

public sealed record RequestEmailChangeDbInput
{
    public long     UserId       { get; init; }
    public string   NewEmail     { get; init; } = default!;
    public string   TokenHash    { get; init; } = default!;
    public DateTime ExpiresAtUtc { get; init; }
    public string?  CreatedByIp  { get; init; }
}

public sealed record ConfirmEmailChangeDbInput
{
    public string  TokenHash  { get; init; } = default!;
    public string? UsedByIp   { get; init; }
}

public sealed record ConfirmEmailChangeDbResult(
    byte     ResultCode,
    long?    UserId,
    string?  OldEmail,
    string?  NewEmail,
    string?  DisplayNameEn,
    string?  DisplayNameAr
);
