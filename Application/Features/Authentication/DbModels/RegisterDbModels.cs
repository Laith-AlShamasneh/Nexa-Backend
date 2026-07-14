using Shared.Enums.Identity;

namespace Application.Features.Authentication.DbModels;

public sealed class RegisterDbInput
{
    public string       FirstNameEn    { get; init; } = string.Empty;
    public string       LastNameEn     { get; init; } = string.Empty;
    public string?      FirstNameAr    { get; init; }
    public string?      LastNameAr     { get; init; }
    public string       DisplayNameEn  { get; init; } = string.Empty;
    public string?      DisplayNameAr  { get; init; }
    public DateOnly?    DateOfBirth    { get; init; }
    public GenderTypes? GenderId       { get; init; }
    public string?      ProfilePicture { get; init; }
    public string       Email          { get; init; } = string.Empty;
    public string       PasswordHash   { get; init; } = string.Empty;
    public int          DefaultRoleId  { get; init; }
}

public sealed class RegisterDbResult
{
    public long    UserId        { get; init; }
    public long    PersonId      { get; init; }
    public string  Email         { get; init; } = string.Empty;
    public string  DisplayNameEn { get; init; } = string.Empty;
    public string? DisplayNameAr { get; init; }
    public string? ProfilePicture { get; init; }
    public int     RoleId        { get; init; }
    public string  RoleNameEn    { get; init; } = string.Empty;
    public string  RoleNameAr    { get; init; } = string.Empty;
}

public sealed class SaveRefreshTokenDbInput
{
    public long     UserId       { get; init; }
    public string   Token        { get; init; } = string.Empty;
    public DateTime ExpiresOnUtc { get; init; }
    public string?  CreatedByIp  { get; init; }
}
