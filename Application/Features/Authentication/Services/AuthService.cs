using Application.Common.Constants;
using Application.Common.Options;
using Application.Features.Authentication.DbModels;
using Application.Features.Authentication.DTOs;
using Application.Features.Email.Jobs;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shared.Constants;
using Shared.Enums.System;
using Shared.Results;
using Application.Features.Onboarding.DTOs;

namespace Application.Features.Authentication.Services;

internal sealed class AuthService(
    IAuthRepository                 authRepository,
    IPasswordHasher                 passwordHasher,
    IJwtService                     jwtService,
    ITokenHasher                    tokenHasher,
    IFileService                    fileService,
    IStorageUtility                 storageUtility,
    IUserContext                    userContext,
    IMessageProvider                messageProvider,
    IBackgroundJobService           backgroundJobService,
    INotificationPublisher          notificationPublisher,
    IOptions<AuthenticationOptions> authOptions,
    IOnboardingService              onboardingService,
    ICacheService                   cacheService,
    ILogger<AuthService>            logger) : IAuthService
{
    private const int RefreshTokenExpiryDays = 7;

    // Process-wide constant hash used to equalize password-verify timing on the
    // "email not found" path, so response time can't be used to enumerate accounts.
    private static string? _decoyPasswordHash;

    public async Task<ServiceResult<RegisterResponse>> RegisterAsync(
        RegisterRequest request, CancellationToken ct = default)
    {
        // 1. Fast email duplicate check before any expensive work
        var emailExists = await authRepository.CheckEmailExistsAsync(request.Email, ct);
        if (emailExists)
        {
            var failMsg    = await messageProvider.GetMessagesAsync(MessageKeys.Authentication.RegistrationFailed, ct);
            var detailMsg  = await messageProvider.GetMessagesAsync(MessageKeys.Authentication.EmailAlreadyInUse, ct);
            return ServiceResultFactory.Failure<RegisterResponse>(InternalResponseCodes.Conflict, failMsg, [detailMsg]);
        }

        // 2. Upload profile image if provided
        string? profilePictureFileName = null;
        if (request.ProfileImage is not null)
        {
            var ext = Path.GetExtension(request.ProfileImage.FileName);
            profilePictureFileName = $"{Guid.NewGuid()}{ext}";
            var fileKey = storageUtility.BuildFileKey(FolderPaths.ProfilePictures, profilePictureFileName);
            await using var stream = request.ProfileImage.Content;
            await fileService.UploadAsync(stream, fileKey, request.ProfileImage.ContentType, ct);
        }

        // 3. Persist Person + User + UserRole atomically
        var dbInput = new RegisterDbInput
        {
            FirstNameEn    = request.FirstNameEn,
            LastNameEn     = request.LastNameEn,
            FirstNameAr    = request.FirstNameAr,
            LastNameAr     = request.LastNameAr,
            DisplayNameEn  = request.DisplayNameEn,
            DisplayNameAr  = request.DisplayNameAr,
            DateOfBirth    = request.DateOfBirth,
            GenderId       = request.GenderId,
            ProfilePicture = profilePictureFileName,
            Email          = request.Email,
            PasswordHash   = passwordHasher.Hash(request.Password),
            DefaultRoleId  = (int)SystemRoles.User
        };

        var dbResult = await authRepository.RegisterAsync(dbInput, ct);

        if (dbResult is null)
        {
            // Race-condition duplicate — clean up uploaded file
            if (profilePictureFileName is not null)
                await fileService.DeleteAsync(storageUtility.BuildFileKey(FolderPaths.ProfilePictures, profilePictureFileName), ct);

            var failMsg   = await messageProvider.GetMessagesAsync(MessageKeys.Authentication.RegistrationFailed, ct);
            var detailMsg = await messageProvider.GetMessagesAsync(MessageKeys.Authentication.EmailAlreadyInUse, ct);
            return ServiceResultFactory.Failure<RegisterResponse>(InternalResponseCodes.Conflict, failMsg, [detailMsg]);
        }

        // 3.5 Initialise onboarding for the new user (non-critical — registration succeeds even on failure)
        try
        {
            await onboardingService.InitializeAsync(dbResult.UserId, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Onboarding initialisation failed for new user {UserId}; registration continues.", dbResult.UserId);
        }

        // 4. Generate tokens
        var jwtModel = new JwtTokenResponse(
            dbResult.UserId, dbResult.Email, dbResult.DisplayNameEn, [(int)SystemRoles.User],
            await GetSecurityStampAsync(dbResult.UserId, ct));

        var (accessToken, accessTokenExpiresAt) = jwtService.GenerateAccessToken(jwtModel);

        var rawRefreshToken       = tokenHasher.GenerateRawToken();
        var hashedRefreshToken    = tokenHasher.Hash(rawRefreshToken);
        var refreshTokenExpiresAt = DateTime.UtcNow.AddDays(RefreshTokenExpiryDays);

        await authRepository.SaveRefreshTokenAsync(new SaveRefreshTokenDbInput
        {
            UserId       = dbResult.UserId,
            Token        = hashedRefreshToken,
            ExpiresOnUtc = refreshTokenExpiresAt,
            CreatedByIp  = userContext.IpAddress
        }, ct);

        // 5. Localize display name and role name
        var isArabic    = userContext.Language == SystemLanguages.Arabic;
        var displayName = isArabic && !string.IsNullOrWhiteSpace(dbResult.DisplayNameAr)
            ? dbResult.DisplayNameAr
            : dbResult.DisplayNameEn;
        var roleName = isArabic ? dbResult.RoleNameAr : dbResult.RoleNameEn;

        // 6. Build profile image URL
        string? profileImageUrl = null;
        if (profilePictureFileName is not null)
        {
            var (url, _) = storageUtility.BuildFilePathWithExpiration(
                FolderPaths.ProfilePictures,
                profilePictureFileName,
                isInternalStorage: true,
                baseUrl: userContext.RequestBaseUrl);
            profileImageUrl = url;
        }

        // 7. Generate and persist email confirmation token (raw sent to user; only hash stored — Rule 17)
        var rawConfirmToken    = tokenHasher.GenerateRawToken();
        var hashedConfirmToken = tokenHasher.Hash(rawConfirmToken);
        var confirmTokenExpiry = DateTime.UtcNow.AddHours(authOptions.Value.EmailConfirmationExpiryHours);

        await authRepository.SaveConfirmationTokenAsync(new SaveConfirmationTokenDbInput
        {
            UserId       = dbResult.UserId,
            TokenHash    = hashedConfirmToken,
            ExpiresAtUtc = confirmTokenExpiry,
            CreatedByIp  = userContext.IpAddress
        }, ct);

        var confirmationLink = $"{authOptions.Value.ConfirmEmailBaseUrl}?token={Uri.EscapeDataString(rawConfirmToken)}";

        // 8. Enqueue welcome email and confirmation email via durable background jobs (Rule 16)
        await backgroundJobService.EnqueueAsync(
            jobType: JobTypes.WelcomeEmail,
            payload: new WelcomeEmailPayload(dbResult.Email, displayName, userContext.Language),
            ct: ct);

        await backgroundJobService.EnqueueAsync(
            jobType: JobTypes.EmailConfirmation,
            payload: new EmailConfirmationPayload(dbResult.Email, displayName, confirmationLink, userContext.Language),
            ct: ct);

        await notificationPublisher.PublishAsync(NotificationCodes.Welcome, dbResult.UserId, ct: ct);

        var successMsg = await messageProvider.GetMessagesAsync(MessageKeys.Authentication.UserRegisteredSuccess, ct);
        var response   = new RegisterResponse(
            Email:                  dbResult.Email,
            DisplayName:            displayName,
            ProfileImageUrl:        profileImageUrl,
            Roles:                  [roleName],
            AccessToken:            accessToken,
            RefreshToken:           rawRefreshToken,
            AccessTokenExpiresAt:   accessTokenExpiresAt,
            RefreshTokenExpiresAt:  refreshTokenExpiresAt,
            HasCompletedOnboarding: false);

        return ServiceResultFactory.Success(response, InternalResponseCodes.Created, successMsg);
    }

    public async Task<ServiceResult<LoginResponse>> LoginAsync(
        LoginRequest request, CancellationToken ct = default)
    {
        // 1. Load user record — NULL means email does not exist
        var user = await authRepository.GetByEmailForLoginAsync(request.Email, ct);

        // Always return generic failure — never reveal whether the email exists (Rule: no enumeration).
        // Run a decoy verify so the no-such-user path costs the same as a wrong-password path
        // (otherwise response timing leaks whether the email is registered).
        if (user is null)
        {
            var decoyHash = _decoyPasswordHash ??= passwordHasher.Hash("decoy-timing-equalizer");
            passwordHasher.Verify(request.Password, decoyHash);

            return ServiceResultFactory.Failure<LoginResponse>(
                InternalResponseCodes.Unauthorized,
                await messageProvider.GetMessagesAsync(MessageKeys.Authentication.InvalidCredentials, ct));
        }

        // 2. Inactive account — block before any further processing
        if (!user.IsActive)
        {
            return ServiceResultFactory.Failure<LoginResponse>(
                InternalResponseCodes.Unauthorized,
                await messageProvider.GetMessagesAsync(MessageKeys.Authentication.AccountNotActive, ct));
        }

        // 3. Lockout check — respect temporal locks; expired locks are cleared on the next successful login
        var isCurrentlyLocked = user.IsLocked &&
            (user.LockoutEndDateUtc is null || user.LockoutEndDateUtc > DateTime.UtcNow);

        if (isCurrentlyLocked)
        {
            return ServiceResultFactory.Failure<LoginResponse>(
                InternalResponseCodes.Unauthorized,
                await messageProvider.GetMessagesAsync(MessageKeys.Authentication.AccountLocked, ct));
        }

        // 4. Password verification — wrong password increments the failed-attempt counter atomically
        if (!passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            var options = authOptions.Value;
            await authRepository.UpdateLoginAsync(new LoginUpdateDbModel
            {
                UserId                 = user.UserId,
                LoginSucceeded         = false,
                MaxFailedAttempts      = options.MaxFailedLoginAttempts,
                LockoutDurationMinutes = options.LockoutDurationMinutes
            }, ct);

            return ServiceResultFactory.Failure<LoginResponse>(
                InternalResponseCodes.Unauthorized,
                await messageProvider.GetMessagesAsync(MessageKeys.Authentication.InvalidCredentials, ct));
        }

        // 5. Email confirmation — password was correct so do not penalize the attempt counter
        if (!user.IsEmailConfirmed)
        {
            return ServiceResultFactory.Failure<LoginResponse>(
                InternalResponseCodes.Unauthorized,
                await messageProvider.GetMessagesAsync(MessageKeys.Authentication.EmailNotConfirmed, ct));
        }

        // 6. Mark login success — resets failed-attempt counter and updates LastLoginDateUtc atomically
        await authRepository.UpdateLoginAsync(new LoginUpdateDbModel
        {
            UserId                 = user.UserId,
            LoginSucceeded         = true,
            MaxFailedAttempts      = authOptions.Value.MaxFailedLoginAttempts,
            LockoutDurationMinutes = authOptions.Value.LockoutDurationMinutes
        }, ct);

        // 7. Generate access token
        var jwtModel = new JwtTokenResponse(
            user.UserId, user.Email, user.DisplayNameEn, [user.RoleId],
            await GetSecurityStampAsync(user.UserId, ct));

        var (accessToken, accessTokenExpiresAt) = jwtService.GenerateAccessToken(jwtModel);

        // 8. Generate and persist refresh token (store only the SHA-256 hash — Rule 17)
        var rawRefreshToken       = tokenHasher.GenerateRawToken();
        var hashedRefreshToken    = tokenHasher.Hash(rawRefreshToken);
        var refreshTokenExpiresAt = DateTime.UtcNow.AddDays(RefreshTokenExpiryDays);

        await authRepository.SaveRefreshTokenAsync(new SaveRefreshTokenDbInput
        {
            UserId       = user.UserId,
            Token        = hashedRefreshToken,
            ExpiresOnUtc = refreshTokenExpiresAt,
            CreatedByIp  = userContext.IpAddress
        }, ct);

        // 9. Resolve localized display name and role name
        var isArabic    = userContext.Language == SystemLanguages.Arabic;
        var displayName = isArabic && !string.IsNullOrWhiteSpace(user.DisplayNameAr)
            ? user.DisplayNameAr
            : user.DisplayNameEn;
        var roleName = isArabic ? user.RoleNameAr : user.RoleNameEn;

        // 10. Build profile image URL if present
        string? profileImageUrl = null;
        if (!string.IsNullOrEmpty(user.ProfilePicture))
        {
            var (url, _) = storageUtility.BuildFilePathWithExpiration(
                FolderPaths.ProfilePictures,
                user.ProfilePicture,
                isInternalStorage: true,
                baseUrl: userContext.RequestBaseUrl);
            profileImageUrl = url;
        }

        var loginResponse = new LoginResponse(
            Email:                  user.Email,
            DisplayName:            displayName,
            ProfileImageUrl:        profileImageUrl,
            Roles:                  [roleName],
            AccessToken:            accessToken,
            RefreshToken:           rawRefreshToken,
            AccessTokenExpiresAt:   accessTokenExpiresAt,
            RefreshTokenExpiresAt:  refreshTokenExpiresAt,
            HasCompletedOnboarding: user.OnboardingCompletedAtUtc.HasValue);

        return ServiceResultFactory.Success(
            loginResponse,
            InternalResponseCodes.OK,
            await messageProvider.GetMessagesAsync(MessageKeys.Authentication.UserLoginSuccess, ct));
    }

    public async Task<ServiceResult<bool>> ConfirmEmailAsync(
        ConfirmEmailRequest request, CancellationToken ct = default)
    {
        var tokenHash = tokenHasher.Hash(request.Token);

        var dbResult = await authRepository.ConfirmEmailAsync(new ConfirmEmailDbInput
        {
            TokenHash = tokenHash,
            UsedByIp  = userContext.IpAddress
        }, ct);

        return dbResult.ResultCode switch
        {
            // 0 = success
            0 => ServiceResultFactory.Success(
                    true,
                    InternalResponseCodes.OK,
                    await messageProvider.GetMessagesAsync(MessageKeys.Authentication.EmailConfirmed, ct)),

            // 4 = already confirmed — idempotent, still a success
            4 => ServiceResultFactory.Success(
                    true,
                    InternalResponseCodes.OK,
                    await messageProvider.GetMessagesAsync(MessageKeys.Authentication.EmailAlreadyConfirmed, ct)),

            // 2 = expired
            2 => ServiceResultFactory.Failure<bool>(
                    InternalResponseCodes.BadRequest,
                    await messageProvider.GetMessagesAsync(MessageKeys.Authentication.TokenExpired, ct)),

            // 1 = not found, 3 = already used — same response (no information leak)
            _ => ServiceResultFactory.Failure<bool>(
                    InternalResponseCodes.BadRequest,
                    await messageProvider.GetMessagesAsync(MessageKeys.Authentication.InvalidToken, ct))
        };
    }

    public async Task<ServiceResult<bool>> ResendConfirmationEmailAsync(
        ResendConfirmationEmailRequest request, CancellationToken ct = default)
    {
        // No-enumeration: always return the same success response regardless of whether the email exists
        var successMsg = await messageProvider.GetMessagesAsync(MessageKeys.Authentication.ConfirmationEmailSent, ct);

        var user = await authRepository.GetUserConfirmationStatusAsync(request.Email, ct);

        // User not found, already confirmed, or inactive — silently return success (no enumeration)
        if (user is null || user.IsEmailConfirmed || !user.IsActive)
            return ServiceResultFactory.Success(true, InternalResponseCodes.OK, successMsg);

        // Generate new token, invalidate previous ones
        var rawToken    = tokenHasher.GenerateRawToken();
        var hashedToken = tokenHasher.Hash(rawToken);
        var expiresAt   = DateTime.UtcNow.AddHours(authOptions.Value.EmailConfirmationExpiryHours);

        await authRepository.SaveConfirmationTokenAsync(new SaveConfirmationTokenDbInput
        {
            UserId       = user.UserId,
            TokenHash    = hashedToken,
            ExpiresAtUtc = expiresAt,
            CreatedByIp  = userContext.IpAddress
        }, ct);

        var confirmationLink = $"{authOptions.Value.ConfirmEmailBaseUrl}?token={Uri.EscapeDataString(rawToken)}";

        var isArabic    = userContext.Language == SystemLanguages.Arabic;
        var displayName = isArabic && !string.IsNullOrWhiteSpace(user.DisplayNameAr)
            ? user.DisplayNameAr
            : user.DisplayNameEn;

        await backgroundJobService.EnqueueAsync(
            jobType: JobTypes.EmailConfirmation,
            payload: new EmailConfirmationPayload(request.Email, displayName, confirmationLink, userContext.Language),
            ct: ct);

        return ServiceResultFactory.Success(true, InternalResponseCodes.OK, successMsg);
    }

    public async Task<ServiceResult<bool>> ChangePasswordAsync(
        ChangePasswordRequest request, CancellationToken ct = default)
    {
        // 1. Load the authenticated user's record — keyed by JWT claim, never by request body
        var user = await authRepository.GetUserForChangePasswordAsync(userContext.UserId, ct);

        if (user is null || !user.IsActive)
            return ServiceResultFactory.Failure<bool>(
                InternalResponseCodes.Unauthorized,
                await messageProvider.GetMessagesAsync(MessageKeys.Common.Unauthorized, ct));

        // 2. Verify the supplied current password against the stored BCrypt hash
        if (!passwordHasher.Verify(request.CurrentPassword, user.PasswordHash))
            return ServiceResultFactory.Failure<bool>(
                InternalResponseCodes.BadRequest,
                await messageProvider.GetMessagesAsync(MessageKeys.Authentication.CurrentPasswordIncorrect, ct));

        // 3. Prevent reuse — new password must differ from the current one
        if (passwordHasher.Verify(request.NewPassword, user.PasswordHash))
            return ServiceResultFactory.Failure<bool>(
                InternalResponseCodes.BadRequest,
                await messageProvider.GetMessagesAsync(MessageKeys.Authentication.NewPasswordSameAsCurrent, ct));

        // 4. Hash the new password. If the caller passes the current refresh token,
        //    hash it so the SP can keep that session alive and revoke all others.
        var newHash           = passwordHasher.Hash(request.NewPassword);
        var currentTokenHash  = request.CurrentRefreshToken is not null
            ? tokenHasher.Hash(request.CurrentRefreshToken)
            : (string?)null;

        var dbResult = await authRepository.ChangePasswordAsync(new ChangePasswordDbInput
        {
            UserId           = user.UserId,
            NewPasswordHash  = newHash,
            ChangedByIp      = userContext.IpAddress,
            CurrentTokenHash = currentTokenHash
        }, ct);

        if (dbResult.ResultCode != 0)
            return ServiceResultFactory.Failure<bool>(
                InternalResponseCodes.Unauthorized,
                await messageProvider.GetMessagesAsync(MessageKeys.Common.Unauthorized, ct));

        // 4.5 Bump the security stamp so every outstanding access token for this user
        //     is rejected on its next request (when stamp validation is enabled).
        if (authOptions.Value.ValidateAccessTokenStamp)
        {
            await authRepository.BumpSecurityStampAsync(user.UserId, ct);
            await cacheService.RemoveAsync($"sstamp:{user.UserId}");
        }

        // 5. Enqueue security notification email via durable background job (Rule 16)
        var isArabic    = userContext.Language == SystemLanguages.Arabic;
        var displayName = isArabic && !string.IsNullOrWhiteSpace(user.DisplayNameAr)
            ? user.DisplayNameAr
            : user.DisplayNameEn;

        var changeTime = DateTime.UtcNow.ToString("dd MMM yyyy HH:mm 'UTC'");

        await backgroundJobService.EnqueueAsync(
            jobType:  JobTypes.PasswordChangedEmail,
            payload:  new PasswordChangedEmailPayload(userContext.Email, displayName, changeTime, userContext.Language),
            priority: 1,   // High — security notification
            ct:       ct);

        await notificationPublisher.PublishAsync(
            NotificationCodes.PasswordChanged,
            userContext.UserId,
            parameters: new Dictionary<string, string> { { "ChangedAt", changeTime } },
            ct: ct);

        return ServiceResultFactory.Success(
            true,
            InternalResponseCodes.OK,
            await messageProvider.GetMessagesAsync(MessageKeys.Authentication.PasswordChanged, ct));
    }

    public async Task<ServiceResult<bool>> ForgotPasswordAsync(
        ForgotPasswordRequest request, CancellationToken ct = default)
    {
        // No-enumeration: always return the same success response
        var successMsg = await messageProvider.GetMessagesAsync(MessageKeys.Authentication.ResetEmailSent, ct);

        var user = await authRepository.GetUserForPasswordResetAsync(request.Email, ct);

        // User not found or inactive — silently return success (no user enumeration)
        if (user is null || !user.IsActive)
            return ServiceResultFactory.Success(true, InternalResponseCodes.OK, successMsg);

        // Generate raw token (sent to user), store only SHA-256 hash (Rule 17)
        var rawToken    = tokenHasher.GenerateRawToken();
        var hashedToken = tokenHasher.Hash(rawToken);
        var expiresAt   = DateTime.UtcNow.AddMinutes(authOptions.Value.PasswordResetExpiryMinutes);

        await authRepository.SavePasswordResetTokenAsync(new SavePasswordResetTokenDbInput
        {
            UserId       = user.UserId,
            TokenHash    = hashedToken,
            ExpiresAtUtc = expiresAt,
            CreatedByIp  = userContext.IpAddress
        }, ct);

        var resetLink = $"{authOptions.Value.ResetPasswordBaseUrl}?token={Uri.EscapeDataString(rawToken)}";

        var isArabic    = userContext.Language == SystemLanguages.Arabic;
        var displayName = isArabic && !string.IsNullOrWhiteSpace(user.DisplayNameAr)
            ? user.DisplayNameAr
            : user.DisplayNameEn;

        // Enqueue password reset email at high priority via durable background job (Rule 16)
        await backgroundJobService.EnqueueAsync(
            jobType:  JobTypes.PasswordResetEmail,
            payload:  new PasswordResetEmailPayload(request.Email, displayName, resetLink, userContext.Language),
            priority: 1,   // High
            ct:       ct);

        return ServiceResultFactory.Success(true, InternalResponseCodes.OK, successMsg);
    }

    public async Task<ServiceResult<bool>> ValidateResetTokenAsync(
        ValidateResetTokenRequest request, CancellationToken ct = default)
    {
        var tokenHash = tokenHasher.Hash(request.Token);

        var dbResult = await authRepository.ValidatePasswordResetTokenAsync(tokenHash, ct);

        return dbResult.ResultCode switch
        {
            0 => ServiceResultFactory.Success(
                    true,
                    InternalResponseCodes.OK,
                    await messageProvider.GetMessagesAsync(MessageKeys.Authentication.ResetTokenValid, ct)),

            2 => ServiceResultFactory.Failure<bool>(
                    InternalResponseCodes.BadRequest,
                    await messageProvider.GetMessagesAsync(MessageKeys.Authentication.TokenExpired, ct)),

            // 1=NotFound, 3=AlreadyUsed, 4=UserInactive — all map to InvalidResetToken (no leak)
            _ => ServiceResultFactory.Failure<bool>(
                    InternalResponseCodes.BadRequest,
                    await messageProvider.GetMessagesAsync(MessageKeys.Authentication.InvalidResetToken, ct))
        };
    }

    public async Task<ServiceResult<bool>> ResetPasswordAsync(
        ResetPasswordRequest request, CancellationToken ct = default)
    {
        var tokenHash    = tokenHasher.Hash(request.Token);
        var passwordHash = passwordHasher.Hash(request.NewPassword);

        var dbResult = await authRepository.ResetPasswordAsync(new ResetPasswordDbInput
        {
            TokenHash    = tokenHash,
            PasswordHash = passwordHash,
            UsedByIp     = userContext.IpAddress
        }, ct);

        return dbResult.ResultCode switch
        {
            0 => ServiceResultFactory.Success(
                    true,
                    InternalResponseCodes.OK,
                    await messageProvider.GetMessagesAsync(MessageKeys.Authentication.PasswordResetSuccess, ct)),

            2 => ServiceResultFactory.Failure<bool>(
                    InternalResponseCodes.BadRequest,
                    await messageProvider.GetMessagesAsync(MessageKeys.Authentication.TokenExpired, ct)),

            // 1=NotFound, 3=AlreadyUsed, 4=UserInactive — all map to InvalidResetToken (no leak)
            _ => ServiceResultFactory.Failure<bool>(
                    InternalResponseCodes.BadRequest,
                    await messageProvider.GetMessagesAsync(MessageKeys.Authentication.InvalidResetToken, ct))
        };
    }

    public async Task<ServiceResult<LoginResponse>> RefreshTokenAsync(
        RefreshTokenRequest request, CancellationToken ct = default)
    {
        var oldHash   = tokenHasher.Hash(request.RefreshToken!);
        var newRaw    = tokenHasher.GenerateRawToken();
        var newHash   = tokenHasher.Hash(newRaw);
        var newExpiry = DateTime.UtcNow.AddDays(RefreshTokenExpiryDays);

        var dbResult = await authRepository.RefreshTokenAsync(new RefreshTokenDbInput
        {
            OldTokenHash    = oldHash,
            NewTokenHash    = newHash,
            NewExpiresOnUtc = newExpiry,
            RevokedByIp     = userContext.IpAddress
        }, ct);

        if (dbResult.ResultCode == 2)
            return ServiceResultFactory.Failure<LoginResponse>(
                InternalResponseCodes.Unauthorized,
                await messageProvider.GetMessagesAsync(MessageKeys.Authentication.TokenExpired, ct));

        if (dbResult.ResultCode != 0)
            return ServiceResultFactory.Failure<LoginResponse>(
                InternalResponseCodes.Unauthorized,
                await messageProvider.GetMessagesAsync(MessageKeys.Authentication.InvalidToken, ct));

        var jwtModel = new JwtTokenResponse(
            dbResult.UserId, dbResult.Email!, dbResult.DisplayNameEn!, [dbResult.RoleId],
            await GetSecurityStampAsync(dbResult.UserId, ct));

        var (accessToken, accessTokenExpiresAt) = jwtService.GenerateAccessToken(jwtModel);

        var isArabic    = userContext.Language == SystemLanguages.Arabic;
        var displayName = isArabic && !string.IsNullOrWhiteSpace(dbResult.DisplayNameAr)
            ? dbResult.DisplayNameAr
            : dbResult.DisplayNameEn!;
        var roleName = isArabic ? dbResult.RoleNameAr! : dbResult.RoleNameEn!;

        string? profileImageUrl = null;
        if (!string.IsNullOrEmpty(dbResult.ProfilePicture))
        {
            var (url, _) = storageUtility.BuildFilePathWithExpiration(
                FolderPaths.ProfilePictures,
                dbResult.ProfilePicture,
                isInternalStorage: true,
                baseUrl: userContext.RequestBaseUrl);
            profileImageUrl = url;
        }

        var response = new LoginResponse(
            Email:                  dbResult.Email!,
            DisplayName:            displayName,
            ProfileImageUrl:        profileImageUrl,
            Roles:                  [roleName],
            AccessToken:            accessToken,
            RefreshToken:           newRaw,
            AccessTokenExpiresAt:   accessTokenExpiresAt,
            RefreshTokenExpiresAt:  newExpiry,
            HasCompletedOnboarding: dbResult.OnboardingCompletedAtUtc.HasValue);

        return ServiceResultFactory.Success(
            response,
            InternalResponseCodes.OK,
            await messageProvider.GetMessagesAsync(MessageKeys.Authentication.TokenRefreshed, ct));
    }

    public async Task<ServiceResult<bool>> LogoutAsync(
        LogoutRequest request, CancellationToken ct = default)
    {
        if (!string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            var tokenHash = tokenHasher.Hash(request.RefreshToken);
            await authRepository.LogoutAsync(new LogoutDbInput
            {
                TokenHash   = tokenHash,
                RevokedByIp = userContext.IpAddress
            }, ct);
        }

        var msg = await messageProvider.GetMessagesAsync(MessageKeys.Authentication.LogoutSuccess, ct);
        return ServiceResultFactory.Success(true, InternalResponseCodes.OK, msg);
    }

    // ─── Email Change ──────────────────────────────────────────────────────────

    public async Task<ServiceResult<bool>> RequestEmailChangeAsync(
        RequestEmailChangeRequest request, CancellationToken ct = default)
    {
        var profile = await authRepository.GetProfileForEmailChangeAsync(userContext.UserId, ct);

        if (profile is null || !profile.IsActive)
            return ServiceResultFactory.Failure<bool>(
                InternalResponseCodes.Unauthorized,
                await messageProvider.GetMessagesAsync(MessageKeys.Common.Unauthorized, ct));

        if (!passwordHasher.Verify(request.CurrentPassword, profile.PasswordHash))
            return ServiceResultFactory.Failure<bool>(
                InternalResponseCodes.BadRequest,
                await messageProvider.GetMessagesAsync(MessageKeys.Profile.CurrentPasswordIncorrect, ct));

        var newEmail = request.NewEmail.Trim().ToLowerInvariant();

        if (newEmail.Equals(profile.Email, StringComparison.OrdinalIgnoreCase))
            return ServiceResultFactory.Failure<bool>(
                InternalResponseCodes.BadRequest,
                await messageProvider.GetMessagesAsync(MessageKeys.Profile.EmailSameAsCurrent, ct));

        var emailTaken = await authRepository.CheckEmailExistsAsync(newEmail, ct);
        if (emailTaken)
            return ServiceResultFactory.Failure<bool>(
                InternalResponseCodes.Conflict,
                await messageProvider.GetMessagesAsync(MessageKeys.Profile.EmailAlreadyInUse, ct));

        var rawToken    = tokenHasher.GenerateRawToken();
        var hashedToken = tokenHasher.Hash(rawToken);
        var expiresAt   = DateTime.UtcNow.AddHours(authOptions.Value.EmailConfirmationExpiryHours);

        await authRepository.RequestEmailChangeAsync(new RequestEmailChangeDbInput
        {
            UserId       = userContext.UserId,
            NewEmail     = newEmail,
            TokenHash    = hashedToken,
            ExpiresAtUtc = expiresAt,
            CreatedByIp  = userContext.IpAddress
        }, ct);

        var isArabic    = userContext.Language == SystemLanguages.Arabic;
        var displayName = isArabic && !string.IsNullOrWhiteSpace(profile.DisplayNameAr)
            ? profile.DisplayNameAr
            : profile.DisplayNameEn;
        var confirmationLink = BuildEmailChangeLink(authOptions.Value, rawToken);

        await backgroundJobService.EnqueueAsync(
            jobType:  JobTypes.EmailChangeRequested,
            payload:  new EmailChangeRequestedPayload(newEmail, displayName, confirmationLink, profile.Email, userContext.Language),
            priority: 1,
            ct:       ct);

        return ServiceResultFactory.Success(
            true,
            InternalResponseCodes.OK,
            await messageProvider.GetMessagesAsync(MessageKeys.Profile.EmailChangeRequested, ct));
    }

    public async Task<ServiceResult<bool>> ConfirmEmailChangeAsync(
        ConfirmEmailChangeRequest request, CancellationToken ct = default)
    {
        var tokenHash = tokenHasher.Hash(request.Token);

        var dbResult = await authRepository.ConfirmEmailChangeAsync(new ConfirmEmailChangeDbInput
        {
            TokenHash = tokenHash,
            UsedByIp  = userContext.IpAddress
        }, ct);

        if (dbResult.ResultCode == 0 && dbResult.OldEmail is not null && dbResult.NewEmail is not null)
        {
            var isArabic    = userContext.Language == SystemLanguages.Arabic;
            var displayName = isArabic && !string.IsNullOrWhiteSpace(dbResult.DisplayNameAr)
                ? dbResult.DisplayNameAr!
                : dbResult.DisplayNameEn!;
            var changeTime = DateTime.UtcNow.ToString("dd MMM yyyy HH:mm 'UTC'");

            await backgroundJobService.EnqueueAsync(
                jobType:  JobTypes.EmailChanged,
                payload:  new EmailChangedPayload(dbResult.OldEmail, displayName, dbResult.NewEmail, changeTime, userContext.Language),
                priority: 1,
                ct:       ct);

            await notificationPublisher.PublishAsync(
                NotificationCodes.EmailChanged,
                dbResult.UserId!.Value,
                parameters: new Dictionary<string, string> { { "ChangedAt", changeTime } },
                ct: ct);

            return ServiceResultFactory.Success(
                true,
                InternalResponseCodes.OK,
                await messageProvider.GetMessagesAsync(MessageKeys.Profile.EmailChangeConfirmed, ct));
        }

        return dbResult.ResultCode == 2
            ? ServiceResultFactory.Failure<bool>(
                InternalResponseCodes.BadRequest,
                await messageProvider.GetMessagesAsync(MessageKeys.Profile.EmailChangeTokenExpired, ct))
            : ServiceResultFactory.Failure<bool>(
                InternalResponseCodes.BadRequest,
                await messageProvider.GetMessagesAsync(MessageKeys.Profile.EmailChangeInvalidToken, ct));
    }

    public async Task<ServiceResult<bool>> CancelEmailChangeAsync(CancellationToken ct = default)
    {
        await authRepository.CancelEmailChangeAsync(userContext.UserId, ct);

        return ServiceResultFactory.Success(
            true,
            InternalResponseCodes.OK,
            await messageProvider.GetMessagesAsync(MessageKeys.Profile.EmailChangeCancelled, ct));
    }

    // Resolves the per-user security stamp for the access-token claim, but only when
    // stamp validation is enabled — so with the flag off there is no extra DB call and
    // the feature has no dependency on the H8 migration being applied yet.
    private async Task<string?> GetSecurityStampAsync(long userId, CancellationToken ct) =>
        authOptions.Value.ValidateAccessTokenStamp
            ? (await authRepository.GetSecurityStampAsync(userId, ct))?.ToString()
            : null;

    private static string BuildEmailChangeLink(AuthenticationOptions opts, string rawToken)
    {
        var baseUrl        = opts.ConfirmEmailBaseUrl;
        var emailChangeUrl = baseUrl.Replace("confirm-email", "confirm-email-change", StringComparison.OrdinalIgnoreCase);
        return $"{emailChangeUrl}?token={Uri.EscapeDataString(rawToken)}";
    }
}
