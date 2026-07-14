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

    public static class Transaction
    {
        // Validation
        public const string AmountRequired          = "Transaction.AmountRequired";
        public const string AmountMustBePositive    = "Transaction.AmountMustBePositive";
        public const string CategoryRequired        = "Transaction.CategoryRequired";
        public const string InvalidCategory         = "Transaction.InvalidCategory";
        public const string DateRequired            = "Transaction.DateRequired";
        public const string DateCannotBeFuture      = "Transaction.DateCannotBeFuture";
        public const string DescriptionTooLong      = "Transaction.DescriptionTooLong";
        public const string NotesTooLong            = "Transaction.NotesTooLong";
        public const string InvalidTransactionType  = "Transaction.InvalidTransactionType";
        public const string PageNumberInvalid       = "Transaction.PageNumberInvalid";
        public const string PageSizeInvalid         = "Transaction.PageSizeInvalid";
        public const string InvalidSortDirection    = "Transaction.InvalidSortDirection";
        public const string AmountRangeInvalid      = "Transaction.AmountRangeInvalid";
        public const string DateRangeInvalid        = "Transaction.DateRangeInvalid";
        public const string InvalidTransactionId    = "Transaction.InvalidTransactionId";

        // Business
        public const string NotFound         = "Transaction.NotFound";
        public const string Created          = "Transaction.Created";
        public const string Updated          = "Transaction.Updated";
        public const string Deleted          = "Transaction.Deleted";
        public const string SearchSuccess    = "Transaction.SearchSuccess";
        public const string AnalyticsLoaded  = "Transaction.AnalyticsLoaded";
    }

    public static class Category
    {
        public const string NotFound            = "Category.NotFound";
        public const string LoadedSuccessfully  = "Category.LoadedSuccessfully";
    }

    public static class Currency
    {
        // Validation
        public const string CurrencyCodeRequired       = "Currency.CurrencyCodeRequired";
        public const string InvalidCurrencyCode        = "Currency.InvalidCurrencyCode";
        public const string InvalidPreference          = "Currency.InvalidPreference";
        public const string DateRequired               = "Currency.DateRequired";
        public const string InvalidDate                = "Currency.InvalidDate";
        public const string RateMustBePositive         = "Currency.RateMustBePositive";
        public const string AmountMustBeNonNegative    = "Currency.AmountMustBeNonNegative";
        public const string SameCurrencyConversion     = "Currency.SameCurrencyConversion";

        // Business
        public const string CurrencyNotFound           = "Currency.CurrencyNotFound";
        public const string ExchangeRateNotFound       = "Currency.ExchangeRateNotFound";
        public const string InvalidProvider            = "Currency.InvalidProvider";
        public const string PreferencesUpdated         = "Currency.PreferencesUpdated";
        public const string PreferencesLoaded          = "Currency.PreferencesLoaded";
        public const string RateSet                    = "Currency.RateSet";
        public const string ConversionSuccess          = "Currency.ConversionSuccess";
        public const string SyncTriggered              = "Currency.SyncTriggered";
        public const string StatisticsLoaded           = "Currency.StatisticsLoaded";
        public const string DashboardLoaded            = "Currency.DashboardLoaded";
        public const string RatesLoaded                = "Currency.RatesLoaded";
        public const string CurrenciesLoaded           = "Currency.CurrenciesLoaded";
        public const string DuplicateRate              = "Currency.DuplicateRate";
    }

    public static class Dashboard
    {
        public const string LoadedSuccessfully = "Dashboard.LoadedSuccessfully";
    }

    public static class Profile
    {
        // Validation
        public const string FirstNameRequired           = "Profile.FirstNameRequired";
        public const string FirstNameTooLong            = "Profile.FirstNameTooLong";
        public const string LastNameRequired            = "Profile.LastNameRequired";
        public const string LastNameTooLong             = "Profile.LastNameTooLong";
        public const string DisplayNameTooLong          = "Profile.DisplayNameTooLong";
        public const string InvalidGender               = "Profile.InvalidGender";
        public const string CurrentPasswordRequired     = "Profile.CurrentPasswordRequired";
        public const string NewPasswordRequired         = "Profile.NewPasswordRequired";
        public const string NewPasswordTooShort         = "Profile.NewPasswordTooShort";
        public const string NewPasswordUppercase        = "Profile.NewPasswordUppercase";
        public const string NewPasswordLowercase        = "Profile.NewPasswordLowercase";
        public const string NewPasswordDigit            = "Profile.NewPasswordDigit";
        public const string NewPasswordSpecial          = "Profile.NewPasswordSpecial";
        public const string ConfirmNewPasswordRequired  = "Profile.ConfirmNewPasswordRequired";
        public const string NewPasswordMismatch         = "Profile.NewPasswordMismatch";
        public const string InvalidProfilePictureFormat = "Profile.InvalidProfilePictureFormat";
        public const string ProfilePictureTooLarge      = "Profile.ProfilePictureTooLarge";

        // Email change validation
        public const string NewEmailRequired            = "Profile.NewEmailRequired";
        public const string NewEmailInvalid             = "Profile.NewEmailInvalid";
        public const string NewEmailTooLong             = "Profile.NewEmailTooLong";
        public const string EmailChangeTokenRequired    = "Profile.EmailChangeTokenRequired";

        // Business
        public const string NotFound                   = "Profile.NotFound";
        public const string Updated                    = "Profile.Updated";
        public const string PasswordChanged            = "Profile.PasswordChanged";
        public const string CurrentPasswordIncorrect   = "Profile.CurrentPasswordIncorrect";
        public const string NewPasswordSameAsCurrent   = "Profile.NewPasswordSameAsCurrent";
        public const string ProfilePictureUpdated      = "Profile.ProfilePictureUpdated";
        public const string ProfilePictureDeleted      = "Profile.ProfilePictureDeleted";
        public const string GetProfileSuccess          = "Profile.GetProfileSuccess";

        // Email change business
        public const string EmailChangeRequested       = "Profile.EmailChangeRequested";
        public const string EmailChangeConfirmed       = "Profile.EmailChangeConfirmed";
        public const string EmailChangeCancelled       = "Profile.EmailChangeCancelled";
        public const string NoPendingEmailChange       = "Profile.NoPendingEmailChange";
        public const string EmailChangeTokenExpired    = "Profile.EmailChangeTokenExpired";
        public const string EmailChangeInvalidToken    = "Profile.EmailChangeInvalidToken";
        public const string EmailAlreadyInUse          = "Profile.EmailAlreadyInUse";
        public const string EmailSameAsCurrent         = "Profile.EmailSameAsCurrent";

        // Session management business
        public const string GetSessionsSuccess         = "Profile.GetSessionsSuccess";
        public const string SessionRevoked             = "Profile.SessionRevoked";
        public const string AllOtherSessionsRevoked    = "Profile.AllOtherSessionsRevoked";
        public const string SessionNotFound            = "Profile.SessionNotFound";
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

    public static class FinancialIntelligence
    {
        // Insights
        public const string InsightsLoaded             = "FinancialIntelligence.InsightsLoaded";
        public const string InsightMarkedRead          = "FinancialIntelligence.InsightMarkedRead";
        public const string AllInsightsMarkedRead      = "FinancialIntelligence.AllInsightsMarkedRead";
        public const string InsightNotFound            = "FinancialIntelligence.InsightNotFound";

        // Patterns
        public const string PatternsLoaded             = "FinancialIntelligence.PatternsLoaded";

        // Recommendations
        public const string RecommendationsLoaded      = "FinancialIntelligence.RecommendationsLoaded";
        public const string RecommendationApplied      = "FinancialIntelligence.RecommendationApplied";
        public const string RecommendationDismissed    = "FinancialIntelligence.RecommendationDismissed";
        public const string RecommendationNotFound     = "FinancialIntelligence.RecommendationNotFound";

        // Dashboard
        public const string DashboardLoaded            = "FinancialIntelligence.DashboardLoaded";

        // Snapshot
        public const string SnapshotGenerated          = "FinancialIntelligence.SnapshotGenerated";

        // Validation
        public const string InvalidPageNumber          = "FinancialIntelligence.InvalidPageNumber";
        public const string InvalidPageSize            = "FinancialIntelligence.InvalidPageSize";
        public const string InvalidInsightId           = "FinancialIntelligence.InvalidInsightId";
        public const string InvalidRecommendationId    = "FinancialIntelligence.InvalidRecommendationId";
    }

    public static class RecurringTransaction
    {
        // Validation
        public const string NameRequired                 = "RecurringTransaction.NameRequired";
        public const string NameTooLong                  = "RecurringTransaction.NameTooLong";
        public const string AmountRequired               = "RecurringTransaction.AmountRequired";
        public const string AmountMustBePositive         = "RecurringTransaction.AmountMustBePositive";
        public const string CategoryRequired             = "RecurringTransaction.CategoryRequired";
        public const string InvalidTransactionType       = "RecurringTransaction.InvalidTransactionType";
        public const string InvalidFrequency             = "RecurringTransaction.InvalidFrequency";
        public const string StartDateRequired            = "RecurringTransaction.StartDateRequired";
        public const string InvalidStartDate             = "RecurringTransaction.InvalidStartDate";
        public const string EndDateBeforeStartDate       = "RecurringTransaction.EndDateBeforeStartDate";
        public const string CustomIntervalRequired       = "RecurringTransaction.CustomIntervalRequired";
        public const string CustomIntervalMustBePositive = "RecurringTransaction.CustomIntervalMustBePositive";
        public const string CustomUnitRequired           = "RecurringTransaction.CustomUnitRequired";
        public const string InvalidDayOfMonth            = "RecurringTransaction.InvalidDayOfMonth";
        public const string DayOfWeekRequired            = "RecurringTransaction.DayOfWeekRequired";
        public const string InvalidDayOfWeek             = "RecurringTransaction.InvalidDayOfWeek";
        public const string InvalidId                    = "RecurringTransaction.InvalidId";
        public const string PageNumberInvalid            = "RecurringTransaction.PageNumberInvalid";
        public const string PageSizeInvalid              = "RecurringTransaction.PageSizeInvalid";
        public const string DescriptionTooLong           = "RecurringTransaction.DescriptionTooLong";
        public const string NotesTooLong                 = "RecurringTransaction.NotesTooLong";

        // Business
        public const string Created           = "RecurringTransaction.Created";
        public const string Updated           = "RecurringTransaction.Updated";
        public const string Deleted           = "RecurringTransaction.Deleted";
        public const string Paused            = "RecurringTransaction.Paused";
        public const string Resumed           = "RecurringTransaction.Resumed";
        public const string NotFound          = "RecurringTransaction.NotFound";
        public const string AlreadyPaused     = "RecurringTransaction.AlreadyPaused";
        public const string AlreadyActive     = "RecurringTransaction.AlreadyActive";
        public const string CannotResumeExpired = "RecurringTransaction.CannotResumeExpired";
        public const string ListLoaded        = "RecurringTransaction.ListLoaded";
        public const string GetSuccess        = "RecurringTransaction.GetSuccess";
        public const string DashboardLoaded   = "RecurringTransaction.DashboardLoaded";
    }

    public static class Subscription
    {
        // Validation
        public const string ProviderNameRequired  = "Subscription.ProviderNameRequired";
        public const string ProviderNameTooLong   = "Subscription.ProviderNameTooLong";
        public const string WebsiteUrlTooLong     = "Subscription.WebsiteUrlTooLong";
        public const string InvalidWebsiteUrl     = "Subscription.InvalidWebsiteUrl";
        public const string InvalidId             = "Subscription.InvalidId";
        public const string PageNumberInvalid     = "Subscription.PageNumberInvalid";
        public const string PageSizeInvalid       = "Subscription.PageSizeInvalid";

        // Business
        public const string Created     = "Subscription.Created";
        public const string Updated     = "Subscription.Updated";
        public const string Deleted     = "Subscription.Deleted";
        public const string NotFound    = "Subscription.NotFound";
        public const string ListLoaded  = "Subscription.ListLoaded";
        public const string GetSuccess  = "Subscription.GetSuccess";
    }

    public static class Goal
    {
        // Validation
        public const string NameRequired                  = "Goal.NameRequired";
        public const string NameTooLong                   = "Goal.NameTooLong";
        public const string DescriptionTooLong            = "Goal.DescriptionTooLong";
        public const string InvalidGoalType               = "Goal.InvalidGoalType";
        public const string TargetAmountRequired          = "Goal.TargetAmountRequired";
        public const string TargetAmountMustBePositive    = "Goal.TargetAmountMustBePositive";
        public const string InitialAmountCannotBeNegative = "Goal.InitialAmountCannotBeNegative";
        public const string InitialAmountExceedsTarget    = "Goal.InitialAmountExceedsTarget";
        public const string InvalidPriority               = "Goal.InvalidPriority";
        public const string TargetDateMustBeFuture        = "Goal.TargetDateMustBeFuture";
        public const string InvalidId                     = "Goal.InvalidId";
        public const string PageNumberInvalid             = "Goal.PageNumberInvalid";
        public const string PageSizeInvalid               = "Goal.PageSizeInvalid";
        public const string ContributionAmountRequired    = "Goal.ContributionAmountRequired";
        public const string ContributionAmountMustBePositive = "Goal.ContributionAmountMustBePositive";
        public const string ContributionDateRequired      = "Goal.ContributionDateRequired";
        public const string NewAmountMustBePositive       = "Goal.NewAmountMustBePositive";
        public const string InvalidRecurringId            = "Goal.InvalidRecurringId";
        public const string NotesTooLong                  = "Goal.NotesTooLong";

        // Business
        public const string NotFound              = "Goal.NotFound";
        public const string Created               = "Goal.Created";
        public const string Updated               = "Goal.Updated";
        public const string Deleted               = "Goal.Deleted";
        public const string Paused                = "Goal.Paused";
        public const string Resumed               = "Goal.Resumed";
        public const string Completed             = "Goal.Completed";
        public const string ListLoaded            = "Goal.ListLoaded";
        public const string GetSuccess            = "Goal.GetSuccess";
        public const string DashboardLoaded       = "Goal.DashboardLoaded";
        public const string ContributionAdded     = "Goal.ContributionAdded";
        public const string WithdrawalAdded       = "Goal.WithdrawalAdded";
        public const string AdjustmentApplied     = "Goal.AdjustmentApplied";
        public const string NoAdjustmentNeeded    = "Goal.NoAdjustmentNeeded";
        public const string ContributionListLoaded = "Goal.ContributionListLoaded";
        public const string RecurringLinked       = "Goal.RecurringLinked";
        public const string RecurringUnlinked     = "Goal.RecurringUnlinked";
        public const string CannotContributeStatus = "Goal.CannotContributeStatus";
        public const string InsufficientBalance   = "Goal.InsufficientBalance";
        public const string RecurringLinkFailed   = "Goal.RecurringLinkFailed";
        public const string AlreadyCompleted      = "Goal.AlreadyCompleted";
        public const string AlreadyPaused         = "Goal.AlreadyPaused";
        public const string AlreadyActive         = "Goal.AlreadyActive";
    }

    public static class BackgroundJobs
    {
        public const string JobEnqueueFailed = "BackgroundJobs.JobEnqueueFailed";
    }

    public static class CashFlow
    {
        public const string ForecastLoaded          = "CashFlow.ForecastLoaded";
        public const string ForecastNotAvailable    = "CashFlow.ForecastNotAvailable";
        public const string DashboardLoaded         = "CashFlow.DashboardLoaded";
    }

    public static class Budget
    {
        // Validation
        public const string NameRequired            = "Budget.NameRequired";
        public const string NameTooLong             = "Budget.NameTooLong";
        public const string AmountRequired          = "Budget.AmountRequired";
        public const string AmountMustBePositive    = "Budget.AmountMustBePositive";
        public const string PercentageMustBe1To100  = "Budget.PercentageMustBe1To100";
        public const string InvalidBudgetType       = "Budget.InvalidBudgetType";
        public const string InvalidPeriodType       = "Budget.InvalidPeriodType";
        public const string StartDateRequired       = "Budget.StartDateRequired";
        public const string EndDateBeforeStartDate  = "Budget.EndDateBeforeStartDate";
        public const string NotesTooLong            = "Budget.NotesTooLong";
        public const string InvalidId              = "Budget.InvalidId";
        public const string PageNumberInvalid       = "Budget.PageNumberInvalid";
        public const string PageSizeInvalid         = "Budget.PageSizeInvalid";

        // Business
        public const string Created                 = "Budget.Created";
        public const string Updated                 = "Budget.Updated";
        public const string Deleted                 = "Budget.Deleted";
        public const string Paused                  = "Budget.Paused";
        public const string Resumed                 = "Budget.Resumed";
        public const string NotFound                = "Budget.NotFound";
        public const string DuplicateBudget         = "Budget.DuplicateBudget";
        public const string InvalidCategory         = "Budget.InvalidCategory";
        public const string ListLoaded              = "Budget.ListLoaded";
        public const string GetSuccess              = "Budget.GetSuccess";
        public const string DashboardLoaded         = "Budget.DashboardLoaded";
        public const string AnalyticsLoaded         = "Budget.AnalyticsLoaded";
        public const string PeriodsLoaded           = "Budget.PeriodsLoaded";
        public const string AlreadyPaused           = "Budget.AlreadyPaused";
        public const string AlreadyActive           = "Budget.AlreadyActive";
        public const string CannotPauseArchived     = "Budget.CannotPauseArchived";
    }

    public static class Reports
    {
        // Validation
        public const string ReportTypeRequired    = "Reports.ReportTypeRequired";
        public const string InvalidReportType     = "Reports.InvalidReportType";
        public const string LanguageRequired      = "Reports.LanguageRequired";
        public const string InvalidLanguage       = "Reports.InvalidLanguage";
        public const string DateFromRequired      = "Reports.DateFromRequired";
        public const string DateToRequired        = "Reports.DateToRequired";
        public const string InvalidDateRange      = "Reports.InvalidDateRange";
        public const string DateRangeTooLarge     = "Reports.DateRangeTooLarge";

        // Business
        public const string TypesLoaded          = "Reports.TypesLoaded";
        public const string Generated            = "Reports.Generated";
        public const string ListLoaded           = "Reports.ListLoaded";
        public const string NotFound             = "Reports.NotFound";
        public const string NotReady             = "Reports.NotReady";
        public const string Deleted              = "Reports.Deleted";
        public const string DownloadReady        = "Reports.DownloadReady";
    }

    public static class Calendar
    {
        // Validation
        public const string TitleRequired             = "Calendar.TitleRequired";
        public const string TitleTooLong              = "Calendar.TitleTooLong";
        public const string DescriptionTooLong        = "Calendar.DescriptionTooLong";
        public const string EventDateRequired         = "Calendar.EventDateRequired";
        public const string InvalidEventDate          = "Calendar.InvalidEventDate";
        public const string InvalidEventType          = "Calendar.InvalidEventType";
        public const string InvalidPriority           = "Calendar.InvalidPriority";
        public const string NotifyBeforeMustBePositive = "Calendar.NotifyBeforeMustBePositive";
        public const string NotifyBeforeTooLarge      = "Calendar.NotifyBeforeTooLarge";
        public const string ColorHexTooLong           = "Calendar.ColorHexTooLong";
        public const string IconTooLong               = "Calendar.IconTooLong";
        public const string InvalidLinkedEntityId     = "Calendar.InvalidLinkedEntityId";
        public const string InvalidLinkedEntityType   = "Calendar.InvalidLinkedEntityType";
        public const string InvalidEventId            = "Calendar.InvalidEventId";
        public const string WeekStartRequired         = "Calendar.WeekStartRequired";
        public const string InvalidWeekStart          = "Calendar.InvalidWeekStart";
        public const string InvalidYear               = "Calendar.InvalidYear";
        public const string InvalidMonth              = "Calendar.InvalidMonth";
        public const string DaysAheadInvalid          = "Calendar.DaysAheadInvalid";
        public const string PageNumberInvalid         = "Calendar.PageNumberInvalid";
        public const string PageSizeInvalid           = "Calendar.PageSizeInvalid";
        public const string KeywordTooLong            = "Calendar.KeywordTooLong";
        public const string InvalidReminderId         = "Calendar.InvalidReminderId";

        // Business
        public const string EventCreated             = "Calendar.EventCreated";
        public const string EventUpdated             = "Calendar.EventUpdated";
        public const string EventDeleted             = "Calendar.EventDeleted";
        public const string EventCompleted           = "Calendar.EventCompleted";
        public const string EventNotFound            = "Calendar.EventNotFound";
        public const string EventAlreadyCompleted    = "Calendar.EventAlreadyCompleted";
        public const string EventAlreadyCancelled    = "Calendar.EventAlreadyCancelled";
        public const string GetSuccess               = "Calendar.GetSuccess";
        public const string ListLoaded               = "Calendar.ListLoaded";
        public const string DashboardLoaded          = "Calendar.DashboardLoaded";
        public const string AgendaLoaded             = "Calendar.AgendaLoaded";
        public const string SearchResultsLoaded      = "Calendar.SearchResultsLoaded";
        public const string ReminderDismissed        = "Calendar.ReminderDismissed";
        public const string ReminderNotFound         = "Calendar.ReminderNotFound";
        public const string ActiveRemindersLoaded    = "Calendar.ActiveRemindersLoaded";
        public const string ReminderSnoozed          = "Calendar.ReminderSnoozed";
        public const string ReminderSnoozeLimit      = "Calendar.ReminderSnoozeLimit";
        public const string ReminderCannotSnoozeCritical = "Calendar.ReminderCannotSnoozeCritical";
        public const string ReminderCannotDismissCritical = "Calendar.ReminderCannotDismissCritical";
        public const string ReminderOpened           = "Calendar.ReminderOpened";
        public const string ReminderHistoryLoaded    = "Calendar.ReminderHistoryLoaded";
    }

    public static class Receipt
    {
        // Validation
        public const string FileRequired              = "Receipt.FileRequired";
        public const string FileTooLarge              = "Receipt.FileTooLarge";
        public const string InvalidFileType           = "Receipt.InvalidFileType";
        public const string TitleTooLong              = "Receipt.TitleTooLong";
        public const string DescriptionTooLong        = "Receipt.DescriptionTooLong";
        public const string MerchantNameTooLong       = "Receipt.MerchantNameTooLong";
        public const string AmountMustBePositive      = "Receipt.AmountMustBePositive";
        public const string CurrencyCodeTooLong       = "Receipt.CurrencyCodeTooLong";
        public const string NotesTooLong              = "Receipt.NotesTooLong";
        public const string InvalidReceiptId          = "Receipt.InvalidReceiptId";
        public const string InvalidTagId              = "Receipt.InvalidTagId";
        public const string TagNameRequired           = "Receipt.TagNameRequired";
        public const string TagNameTooLong            = "Receipt.TagNameTooLong";
        public const string ColorHexTooLong           = "Receipt.ColorHexTooLong";
        public const string KeywordTooLong            = "Receipt.KeywordTooLong";
        public const string InvalidPageNumber         = "Receipt.InvalidPageNumber";
        public const string InvalidPageSize           = "Receipt.InvalidPageSize";
        public const string InvalidTransactionId      = "Receipt.InvalidTransactionId";

        // Business
        public const string Uploaded                  = "Receipt.Uploaded";
        public const string DuplicateFile             = "Receipt.DuplicateFile";
        public const string CorruptOrUnsupportedFile  = "Receipt.CorruptOrUnsupportedFile";
        public const string Updated                   = "Receipt.Updated";
        public const string Deleted                   = "Receipt.Deleted";
        public const string Archived                  = "Receipt.Archived";
        public const string Restored                  = "Receipt.Restored";
        public const string NotFound                  = "Receipt.NotFound";
        public const string CannotArchiveNonActive    = "Receipt.CannotArchiveNonActive";
        public const string CannotRestoreNonArchived  = "Receipt.CannotRestoreNonArchived";
        public const string TransactionAssigned       = "Receipt.TransactionAssigned";
        public const string TransactionUnlinked       = "Receipt.TransactionUnlinked";
        public const string TransactionNotFound       = "Receipt.TransactionNotFound";
        public const string SearchLoaded              = "Receipt.SearchLoaded";
        public const string GetSuccess                = "Receipt.GetSuccess";
        public const string DashboardLoaded           = "Receipt.DashboardLoaded";
        public const string TagCreated                = "Receipt.TagCreated";
        public const string TagDeleted                = "Receipt.TagDeleted";
        public const string TagDuplicate              = "Receipt.TagDuplicate";
        public const string TagNotFound               = "Receipt.TagNotFound";
        public const string TagsLoaded                = "Receipt.TagsLoaded";
        public const string TagsUpdated               = "Receipt.TagsUpdated";
        public const string DownloadReady             = "Receipt.DownloadReady";
    }

    public static class Workspace
    {
        // Validation
        public const string NameRequired             = "Workspace.NameRequired";
        public const string NameTooLong              = "Workspace.NameTooLong";
        public const string DescriptionTooLong       = "Workspace.DescriptionTooLong";
        public const string InvalidTypeId            = "Workspace.InvalidTypeId";
        public const string CurrencyCodeTooLong      = "Workspace.CurrencyCodeTooLong";
        public const string TimezoneTooLong          = "Workspace.TimezoneTooLong";
        public const string ColorTooLong             = "Workspace.ColorTooLong";
        public const string InvalidWorkspaceId       = "Workspace.InvalidWorkspaceId";
        public const string InvalidRoleId            = "Workspace.InvalidRoleId";
        public const string InvalidTargetUserId      = "Workspace.InvalidTargetUserId";
        public const string EmailRequired            = "Workspace.EmailRequired";
        public const string InvalidEmail             = "Workspace.InvalidEmail";
        public const string TokenRequired            = "Workspace.TokenRequired";
        public const string InvalidInvitationId      = "Workspace.InvalidInvitationId";
        public const string PageNumberInvalid        = "Workspace.PageNumberInvalid";
        public const string PageSizeInvalid          = "Workspace.PageSizeInvalid";

        // Business
        public const string Created                  = "Workspace.Created";
        public const string Updated                  = "Workspace.Updated";
        public const string Deleted                  = "Workspace.Deleted";
        public const string NotFound                 = "Workspace.NotFound";
        public const string Forbidden                = "Workspace.Forbidden";
        public const string ListLoaded               = "Workspace.ListLoaded";
        public const string GetSuccess               = "Workspace.GetSuccess";
        public const string Switched                 = "Workspace.Switched";
        public const string SwitchFailed             = "Workspace.SwitchFailed";
        public const string ContextLoaded            = "Workspace.ContextLoaded";

        // Members
        public const string MemberListLoaded         = "Workspace.MemberListLoaded";
        public const string MemberRoleUpdated        = "Workspace.MemberRoleUpdated";
        public const string MemberSuspended          = "Workspace.MemberSuspended";
        public const string MemberReinstated         = "Workspace.MemberReinstated";
        public const string MemberRemoved            = "Workspace.MemberRemoved";
        public const string MemberLeft               = "Workspace.MemberLeft";
        public const string MemberNotFound           = "Workspace.MemberNotFound";
        public const string CannotModifyOwner        = "Workspace.CannotModifyOwner";
        public const string CannotModifySelf         = "Workspace.CannotModifySelf";
        public const string OwnerCannotLeave         = "Workspace.OwnerCannotLeave";

        // Invitations
        public const string InvitationSent           = "Workspace.InvitationSent";
        public const string InvitationCancelled      = "Workspace.InvitationCancelled";
        public const string InvitationAccepted       = "Workspace.InvitationAccepted";
        public const string InvitationRejected       = "Workspace.InvitationRejected";
        public const string InvitationNotFound       = "Workspace.InvitationNotFound";
        public const string InvitationExpired        = "Workspace.InvitationExpired";
        public const string InvitationAlreadyUsed    = "Workspace.InvitationAlreadyUsed";
        public const string AlreadyMember            = "Workspace.AlreadyMember";
        public const string CannotInviteOwnerRole    = "Workspace.CannotInviteOwnerRole";
        public const string EmailMismatch            = "Workspace.EmailMismatch";
        public const string InvitationListLoaded     = "Workspace.InvitationListLoaded";

        // Permissions
        public const string PermissionsLoaded        = "Workspace.PermissionsLoaded";

        // Activity
        public const string ActivityLoaded           = "Workspace.ActivityLoaded";
    }
}
