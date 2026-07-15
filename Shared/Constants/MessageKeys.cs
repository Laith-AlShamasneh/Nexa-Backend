namespace Shared.Constants;

public static class MessageKeys
{
    public static class Common
    {
        public const string Success             = "Common.Success";
        public const string Created             = "Common.Created";
        public const string Updated             = "Common.Updated";
        public const string Deleted             = "Common.Deleted";
        public const string NotFound            = "Common.NotFound";
        public const string BadRequest          = "Common.BadRequest";
        public const string ValidationError     = "Common.ValidationError";
        public const string Unauthorized        = "Common.Unauthorized";
        public const string Forbidden           = "Common.Forbidden";
        public const string Conflict            = "Common.Conflict";
        public const string InternalServerError = "Common.InternalServerError";
    }

    public static class Authentication
    {
        // Validation
        public const string FirstNameRequired           = "Authentication.FirstNameRequired";
        public const string LastNameRequired            = "Authentication.LastNameRequired";
        public const string EmailRequired               = "Authentication.EmailRequired";
        public const string InvalidEmail                = "Authentication.InvalidEmail";
        public const string PasswordRequired            = "Authentication.PasswordRequired";
        public const string PasswordTooShort            = "Authentication.PasswordTooShort";
        public const string PasswordUppercaseRequired   = "Authentication.PasswordUppercaseRequired";
        public const string PasswordLowercaseRequired   = "Authentication.PasswordLowercaseRequired";
        public const string PasswordDigitRequired       = "Authentication.PasswordDigitRequired";
        public const string PasswordSpecialRequired     = "Authentication.PasswordSpecialRequired";
        public const string ConfirmPasswordRequired     = "Authentication.ConfirmPasswordRequired";
        public const string PasswordMismatch            = "Authentication.PasswordMismatch";
        public const string RefreshTokenRequired        = "Authentication.RefreshTokenRequired";
        public const string ResetTokenRequired          = "Authentication.ResetTokenRequired";
        public const string NewPasswordRequired         = "Authentication.NewPasswordRequired";

        // Validation (Register-specific)
        public const string FirstNameTooLong              = "Authentication.FirstNameTooLong";
        public const string LastNameTooLong               = "Authentication.LastNameTooLong";
        public const string DisplayNameRequired           = "Authentication.DisplayNameRequired";
        public const string DisplayNameTooLong            = "Authentication.DisplayNameTooLong";
        public const string InvalidDateOfBirth            = "Authentication.InvalidDateOfBirth";
        public const string InvalidProfileImageFormat     = "Authentication.InvalidProfileImageFormat";
        public const string ProfileImageTooLarge          = "Authentication.ProfileImageTooLarge";

        // Business
        public const string RegistrationFailed       = "Authentication.RegistrationFailed";
        public const string EmailAlreadyInUse        = "Authentication.EmailAlreadyInUse";
        public const string InvalidCredentials      = "Authentication.InvalidCredentials";
        public const string AccountLocked           = "Authentication.AccountLocked";
        public const string AccountNotActive        = "Authentication.AccountNotActive";
        public const string EmailNotConfirmed       = "Authentication.EmailNotConfirmed";
        public const string InvalidToken            = "Authentication.InvalidToken";
        public const string TokenExpired            = "Authentication.TokenExpired";
        public const string TokenRefreshed          = "Authentication.TokenRefreshed";
        public const string TokenRevoked            = "Authentication.TokenRevoked";
        public const string ResetEmailSent          = "Authentication.ResetEmailSent";
        public const string InvalidResetToken       = "Authentication.InvalidResetToken";
        public const string PasswordResetSuccess    = "Authentication.PasswordResetSuccess";
        public const string UserLoginSuccess        = "Authentication.UserLoginSuccess";
        public const string UserRegisteredSuccess   = "Authentication.UserRegisteredSuccess";

        // Change password
        public const string CurrentPasswordRequired    = "Authentication.CurrentPasswordRequired";
        public const string CurrentPasswordIncorrect   = "Authentication.CurrentPasswordIncorrect";
        public const string NewPasswordSameAsCurrent   = "Authentication.NewPasswordSameAsCurrent";
        public const string PasswordChanged            = "Authentication.PasswordChanged";

        // Email confirmation
        public const string ConfirmationTokenRequired  = "Authentication.ConfirmationTokenRequired";
        public const string EmailConfirmed             = "Authentication.EmailConfirmed";
        public const string EmailAlreadyConfirmed      = "Authentication.EmailAlreadyConfirmed";
        public const string ConfirmationEmailSent      = "Authentication.ConfirmationEmailSent";

        // Password reset
        public const string ResetTokenValid            = "Authentication.ResetTokenValid";

        // Logout
        public const string LogoutSuccess              = "Authentication.LogoutSuccess";
    }

    public static class Tenancy
    {
        // Validation
        public const string OrganizationNameRequired = "Tenancy.OrganizationNameRequired";
        public const string InvalidOrganizationEmail = "Tenancy.InvalidOrganizationEmail";
        public const string UnsupportedCurrencyCode   = "Tenancy.UnsupportedCurrencyCode";
        public const string UnsupportedLanguageCode   = "Tenancy.UnsupportedLanguageCode";
        public const string UnsupportedTimeZone       = "Tenancy.UnsupportedTimeZone";
        public const string BranchNameRequired        = "Tenancy.BranchNameRequired";
        public const string OwnerFirstNameRequired    = "Tenancy.OwnerFirstNameRequired";
        public const string OwnerLastNameRequired     = "Tenancy.OwnerLastNameRequired";
        public const string OwnerUsernameRequired     = "Tenancy.OwnerUsernameRequired";
        public const string OwnerEmailRequired        = "Tenancy.OwnerEmailRequired";
        public const string InvalidOwnerEmail         = "Tenancy.InvalidOwnerEmail";
        public const string PasswordMismatch          = "Tenancy.PasswordMismatch";
        public const string InvalidLogoFormat         = "Tenancy.InvalidLogoFormat";
        public const string LogoTooLarge              = "Tenancy.LogoTooLarge";

        // Business
        public const string RegistrationSucceeded      = "Tenancy.RegistrationSucceeded";
        public const string RegistrationFailed          = "Tenancy.RegistrationFailed";
        public const string OrganizationSlugConflict    = "Tenancy.OrganizationSlugConflict";
        public const string RoleTemplatesMissing        = "Tenancy.RoleTemplatesMissing";
    }

    // Used by the Authentication feature's email-change sub-flow (verify current
    // password, request/confirm/cancel a pending email change).
    public static class Profile
    {
        public const string CurrentPasswordRequired  = "Profile.CurrentPasswordRequired";
        public const string CurrentPasswordIncorrect = "Profile.CurrentPasswordIncorrect";
        public const string NewEmailRequired         = "Profile.NewEmailRequired";
        public const string NewEmailInvalid          = "Profile.NewEmailInvalid";
        public const string NewEmailTooLong          = "Profile.NewEmailTooLong";
        public const string EmailSameAsCurrent       = "Profile.EmailSameAsCurrent";
        public const string EmailAlreadyInUse        = "Profile.EmailAlreadyInUse";
        public const string EmailChangeTokenRequired = "Profile.EmailChangeTokenRequired";
        public const string EmailChangeRequested     = "Profile.EmailChangeRequested";
        public const string EmailChangeConfirmed     = "Profile.EmailChangeConfirmed";
        public const string EmailChangeCancelled     = "Profile.EmailChangeCancelled";
        public const string EmailChangeTokenExpired  = "Profile.EmailChangeTokenExpired";
        public const string EmailChangeInvalidToken  = "Profile.EmailChangeInvalidToken";
    }

    public static class Notifications
    {
        // Service responses
        public const string ListLoaded         = "Notifications.ListLoaded";
        public const string UnreadCountLoaded  = "Notifications.UnreadCountLoaded";
        public const string MarkedAsRead       = "Notifications.MarkedAsRead";
        public const string AllMarkedAsRead    = "Notifications.AllMarkedAsRead";
        public const string Archived           = "Notifications.Archived";
        public const string Dismissed          = "Notifications.Dismissed";
        public const string Deleted            = "Notifications.Deleted";
        public const string NotFound           = "Notifications.NotFound";
        public const string PreferencesLoaded  = "Notifications.PreferencesLoaded";
        public const string PreferencesUpdated = "Notifications.PreferencesUpdated";

        // Validation
        public const string InvalidNotificationId = "Notifications.InvalidNotificationId";
        public const string InvalidPageSize       = "Notifications.InvalidPageSize";
        public const string InvalidPageNumber     = "Notifications.InvalidPageNumber";
    }
}
