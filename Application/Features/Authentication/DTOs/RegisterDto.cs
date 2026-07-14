using Microsoft.AspNetCore.Http;
using Shared.Enums.Identity;

namespace Application.Features.Authentication.DTOs;

public sealed class RegisterRequest
{
    public string       FirstNameEn   { get; set; } = string.Empty;
    public string       LastNameEn    { get; set; } = string.Empty;
    public string?      FirstNameAr   { get; set; }
    public string?      LastNameAr    { get; set; }
    public string       DisplayNameEn { get; set; } = string.Empty;
    public string?      DisplayNameAr { get; set; }
    public DateOnly?    DateOfBirth   { get; set; }
    public GenderTypes? GenderId      { get; set; }
    public string       Email         { get; set; } = string.Empty;
    public string       Password      { get; set; } = string.Empty;
    public IFormFile?   ProfileImage  { get; set; }
}

public sealed record RegisterResponse(
    string                Email,
    string                DisplayName,
    string?               ProfileImageUrl,
    IReadOnlyList<string> Roles,
    string                AccessToken,
    string                RefreshToken,
    DateTime              AccessTokenExpiresAt,
    DateTime              RefreshTokenExpiresAt,
    bool                  HasCompletedOnboarding
);
