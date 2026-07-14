namespace Application.Features.Authentication.DTOs;

public sealed record LoginRequest(
    string Email,
    string Password
);

public sealed record LoginResponse(
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
