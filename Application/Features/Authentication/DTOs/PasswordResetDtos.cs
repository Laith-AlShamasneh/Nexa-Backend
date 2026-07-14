namespace Application.Features.Authentication.DTOs;

public sealed record ForgotPasswordRequest(string Email);
public sealed record ValidateResetTokenRequest(string Token);
public sealed record ResetPasswordRequest(string Token, string NewPassword, string ConfirmPassword);
