namespace Application.Common.Constants;

public static class JobTypes
{
    public const string WelcomeEmail           = "WelcomeEmail";
    public const string EmailConfirmation      = "EmailConfirmation";
    public const string PasswordResetEmail     = "PasswordResetEmail";
    public const string PasswordChangedEmail   = "PasswordChangedEmail";
    public const string MonthlyReport          = "MonthlyReport";
    public const string EmailChangeRequested   = "EmailChangeRequested";
    public const string EmailChanged           = "EmailChanged";
    public const string GenerateReport         = "GenerateReport";
    public const string ReportCompletedEmail   = "ReportCompletedEmail";

    // ── Notifications ─────────────────────────────────────────────────────────
    public const string CreateNotification = "CreateNotification";

    // ── Financial Intelligence Layer ──────────────────────────────────────────
    public const string DailyFILProcessing   = "DailyFILProcessing";
    public const string HourlyAnomalyCheck   = "HourlyAnomalyCheck";
    public const string MonthlyFILProcessing = "MonthlyFILProcessing";
    public const string SnapshotRecompute    = "SnapshotRecompute";

    // ── Recurring Transactions ────────────────────────────────────────────────
    public const string ProcessRecurringTransactions    = "ProcessRecurringTransactions";
    public const string SendUpcomingPaymentNotification = "SendUpcomingPaymentNotification";

    // ── Goals & Savings ───────────────────────────────────────────────────────
    public const string GoalBehindScheduleCheck    = "GoalBehindScheduleCheck";
    public const string GoalAutoContributionSync   = "GoalAutoContributionSync";

    // ── Cash Flow Forecasting ─────────────────────────────────────────────────
    public const string ComputeCashFlowForecast = "ComputeCashFlowForecast";

    // ── Budgeting ─────────────────────────────────────────────────────────────
    public const string ComputeBudgetSnapshot    = "ComputeBudgetSnapshot";
    public const string BudgetDailyMaintenance   = "BudgetDailyMaintenance";

    // ── Calendar ──────────────────────────────────────────────────────────────
    public const string CalendarReminder = "CalendarReminder";

    // ── Receipts ──────────────────────────────────────────────────────────────
    public const string ProcessReceiptOcr = "ProcessReceiptOcr";

    // ── Currency ──────────────────────────────────────────────────────────────
    public const string ExchangeRateSync       = "ExchangeRateSync";
    public const string ExchangeRateValidation = "ExchangeRateValidation";

    // ── Workspace ─────────────────────────────────────────────────────────────
    public const string WorkspaceInvitationEmail = "WorkspaceInvitationEmail";
}
