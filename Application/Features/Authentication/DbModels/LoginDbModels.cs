namespace Application.Features.Authentication.DbModels;

public sealed class LoginDbResult
{
    public long      UserId              { get; init; }
    public long      PersonId            { get; init; }
    public string    Email               { get; init; } = null!;
    public string    DisplayNameEn       { get; init; } = null!;
    public string?   DisplayNameAr       { get; init; }
    public string?   ProfilePicture      { get; init; }
    public string    PasswordHash        { get; init; } = null!;
    public bool      IsActive            { get; init; }
    public bool      IsEmailConfirmed    { get; init; }
    public bool      IsLocked            { get; init; }
    public DateTime? LockoutEndDateUtc   { get; init; }
    public int       FailedLoginAttempts { get; init; }
    public int       RoleId              { get; init; }
    public string    RoleNameEn               { get; init; } = null!;
    public string    RoleNameAr               { get; init; } = null!;
    public DateTime? OnboardingCompletedAtUtc { get; init; }
}

public sealed class LoginUpdateDbModel
{
    public long UserId                 { get; init; }
    public bool LoginSucceeded         { get; init; }
    public int  MaxFailedAttempts      { get; init; }
    public int  LockoutDurationMinutes { get; init; }
}
