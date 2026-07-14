namespace Application.Features.Authentication.DbModels;

public sealed class LogoutDbInput
{
    public string  TokenHash   { get; init; } = null!;
    public string? RevokedByIp { get; init; }
}

public sealed class RefreshTokenDbInput
{
    public string   OldTokenHash    { get; init; } = null!;
    public string   NewTokenHash    { get; init; } = null!;
    public DateTime NewExpiresOnUtc { get; init; }
    public string?  RevokedByIp     { get; init; }
}

public sealed class RefreshTokenDbResult
{
    public int     ResultCode     { get; init; }   // 0 = Success, 1 = Invalid/Revoked, 2 = Expired
    public long    UserId         { get; init; }
    public string? Email          { get; init; }
    public string? DisplayNameEn  { get; init; }
    public string? DisplayNameAr  { get; init; }
    public string? ProfilePicture { get; init; }
    public int     RoleId         { get; init; }
    public string?   RoleNameEn               { get; init; }
    public string?   RoleNameAr               { get; init; }
    public DateTime? OnboardingCompletedAtUtc { get; init; }
}
