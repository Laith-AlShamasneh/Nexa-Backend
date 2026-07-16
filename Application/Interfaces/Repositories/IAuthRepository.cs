using Application.Features.Authentication.DbModels;

namespace Application.Interfaces.Repositories;

public interface IAuthRepository
{
    Task<bool>              CheckEmailExistsAsync(string email, CancellationToken ct = default);

    Task<GetProfileForEmailChangeDbResult?> GetProfileForEmailChangeAsync(long userId, CancellationToken ct = default);
    Task                                    RequestEmailChangeAsync(RequestEmailChangeDbInput input, CancellationToken ct = default);
    Task<ConfirmEmailChangeDbResult>        ConfirmEmailChangeAsync(ConfirmEmailChangeDbInput input, CancellationToken ct = default);
    Task                                    CancelEmailChangeAsync(long userId, CancellationToken ct = default);
    Task<RegisterDbResult?> RegisterAsync(RegisterDbInput input, CancellationToken ct = default);
    Task                    SaveRefreshTokenAsync(SaveRefreshTokenDbInput input, CancellationToken ct = default);
    Task<LoginDbResult?>    GetByEmailForLoginAsync(string email, CancellationToken ct = default);
    Task                    UpdateLoginAsync(LoginUpdateDbModel model, CancellationToken ct = default);

    // Used only by AuthService.RegisterAsync (out of scope for the Email Confirmation
    // module — see docs/EMAIL_CONFIRMATION.md "Relationship to AuthService"). Confirm
    // and resend for the real, wired-up registration flow (Tenant Onboarding) go
    // through IEmailConfirmationService instead.
    Task SaveConfirmationTokenAsync(SaveConfirmationTokenDbInput input, CancellationToken ct = default);

    Task<ChangePasswordUserDbResult?> GetUserForChangePasswordAsync(long userId, CancellationToken ct = default);
    Task<ChangePasswordDbResult>      ChangePasswordAsync(ChangePasswordDbInput input, CancellationToken ct = default);

    Task<ForgotPasswordDbResult?>    GetUserForPasswordResetAsync(string email, CancellationToken ct = default);
    Task                             SavePasswordResetTokenAsync(SavePasswordResetTokenDbInput input, CancellationToken ct = default);
    Task<ValidateResetTokenDbResult> ValidatePasswordResetTokenAsync(string tokenHash, CancellationToken ct = default);
    Task<ResetPasswordDbResult>      ResetPasswordAsync(ResetPasswordDbInput input, CancellationToken ct = default);

    Task<RefreshTokenDbResult> RefreshTokenAsync(RefreshTokenDbInput input, CancellationToken ct = default);

    Task LogoutAsync(LogoutDbInput input, CancellationToken ct = default);

    // H8 — access-token revocation via per-user security stamp.
    Task<Guid?> GetSecurityStampAsync(long userId, CancellationToken ct = default);
    Task        BumpSecurityStampAsync(long userId, CancellationToken ct = default);
}
