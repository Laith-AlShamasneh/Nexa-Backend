namespace Application.Features.Authentication.DTOs;

public sealed record ChangePasswordRequest(
    string  CurrentPassword,
    string  NewPassword,
    string  ConfirmPassword,
    string? CurrentRefreshToken = null);
