namespace Application.Common.Constants;

/// <summary>
/// Template codes that identify what kind of notification to create.
/// Each code maps to a row in NotificationTemplates and its translations.
/// </summary>
public static class NotificationCodes
{
    // ── Security ─────────────────────────────────────────────────────────────
    public const string Welcome         = "Welcome";
    public const string PasswordChanged = "PasswordChanged";
    public const string EmailChanged    = "EmailChanged";
    public const string SessionRevoked  = "SessionRevoked";

    // ── Financial ─────────────────────────────────────────────────────────────
    public const string LargeTransaction   = "LargeTransaction";
    public const string BudgetExceeded     = "BudgetExceeded";
    public const string BudgetNearingLimit = "BudgetNearingLimit";
    public const string BudgetPeriodReset  = "BudgetPeriodReset";

    // ── Reports ───────────────────────────────────────────────────────────────
    public const string ReportReady  = "ReportReady";
    public const string ReportFailed = "ReportFailed";

    // ── Profile ───────────────────────────────────────────────────────────────
    public const string ProfileUpdated        = "ProfileUpdated";
    public const string ProfilePictureChanged = "ProfilePictureChanged";

    // ── System ────────────────────────────────────────────────────────────────
    public const string SystemAnnouncement = "SystemAnnouncement";
    public const string MaintenanceNotice  = "MaintenanceNotice";

    // ── Goals & Savings ───────────────────────────────────────────────────────
    public const string GoalMilestoneReached = "Goal_MilestoneReached";
    public const string GoalCompleted        = "Goal_Completed";
    public const string GoalBehindSchedule   = "Goal_BehindSchedule";

    // ── Cash Flow Forecasting ─────────────────────────────────────────────────
    public const string CashFlowNegativeBalance = "CASHFLOW_NEGATIVE_BALANCE_RISK";
    public const string CashFlowCashShortage    = "CASHFLOW_CASH_SHORTAGE_WARNING";
    public const string CashFlowGoalAtRisk      = "CASHFLOW_GOAL_AT_RISK";

    // ── Financial Intelligence ─────────────────────────────────────────────────
    public const string FILOverspendingAlert   = "FIL_OverspendingAlert";
    public const string FILSpendingSpike       = "FIL_SpendingSpike";
    public const string FILUnusualTransaction  = "FIL_UnusualTransaction";
    public const string FILHighExpenseRatio    = "FIL_HighExpenseRatio";
    public const string FILAchievement         = "FIL_Achievement";      // kept for backward compat
    public const string FILPositiveBehavior    = "FIL_PositiveBehavior";
    public const string FILConsistentSaver     = "FIL_ConsistentSaver";
    public const string FILMonthlySummary      = "FIL_MonthlySummary";
}
