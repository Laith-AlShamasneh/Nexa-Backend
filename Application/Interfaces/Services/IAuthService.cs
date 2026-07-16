using Application.Features.Authentication.DTOs;
using Shared.Results;

namespace Application.Interfaces.Services;

public interface IAuthService
{
    Task<ServiceResult<RegisterResponse>> RegisterAsync(RegisterRequest request, CancellationToken ct = default);
    Task<ServiceResult<LoginResponse>>    LoginAsync(LoginRequest request, CancellationToken ct = default);
    Task<ServiceResult<bool>>             ChangePasswordAsync(ChangePasswordRequest request, CancellationToken ct = default);
    Task<ServiceResult<bool>>             ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken ct = default);
    Task<ServiceResult<bool>>             ValidateResetTokenAsync(ValidateResetTokenRequest request, CancellationToken ct = default);
    Task<ServiceResult<bool>>             ResetPasswordAsync(ResetPasswordRequest request, CancellationToken ct = default);
    Task<ServiceResult<LoginResponse>>    RefreshTokenAsync(RefreshTokenRequest request, CancellationToken ct = default);
    Task<ServiceResult<bool>>             LogoutAsync(LogoutRequest request, CancellationToken ct = default);

    Task<ServiceResult<bool>> RequestEmailChangeAsync(RequestEmailChangeRequest request, CancellationToken ct = default);
    Task<ServiceResult<bool>> ConfirmEmailChangeAsync(ConfirmEmailChangeRequest request, CancellationToken ct = default);
    Task<ServiceResult<bool>> CancelEmailChangeAsync(CancellationToken ct = default);
}
