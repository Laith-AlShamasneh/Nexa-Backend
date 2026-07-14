using Application.Features.Authentication.Services;
using Application.Features.Workspace.Services;
using Application.Features.Budget;
using Application.Features.Budget.Services;
using Application.Features.Currency.Services;
using Application.Features.Calendar;
using Application.Features.Calendar.Services;
using Application.Features.CashFlow;
using Application.Features.CashFlow.Services;
using Application.Features.Category.Services;
using Application.Features.Dashboard.Services;
using Application.Features.FinancialIntelligence;
using Application.Features.FinancialIntelligence.Rules;
using Application.Features.FinancialIntelligence.Services;
using Application.Features.Notifications;
using Application.Features.Notifications.Services;
using Application.Features.Onboarding.Services;
using Application.Features.Profile.Services;
using Application.Features.Goals;
using Application.Features.Goals.Services;
using Application.Features.Receipt;
using Application.Features.Receipt.Services;
using Application.Features.RecurringTransactions;
using Application.Features.RecurringTransactions.Services;
using Application.Features.Reports;
using Application.Features.Transaction.Services;
using Application.Interfaces.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Application.Common.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IOnboardingService, OnboardingService>();
        services.AddScoped<IProfileService, ProfileService>();
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<ITransactionService, TransactionService>();
        services.AddScoped<ICategoryService, CategoryService>();
        services.AddScoped<IReportService, ReportService>();
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<INotificationPublisher, NotificationPublisher>();
        // FinancialIntelligenceService implements both the API-facing and the
        // background-processing interfaces. Register the concrete type once so
        // both interface resolutions share the same scoped instance.
        services.AddScoped<FinancialIntelligenceService>();
        services.AddScoped<IFinancialIntelligenceService>(
            sp => sp.GetRequiredService<FinancialIntelligenceService>());
        services.AddScoped<IFILBackgroundProcessingService>(
            sp => sp.GetRequiredService<FinancialIntelligenceService>());
        services.AddScoped<IFinancialRulesEngine, FinancialRulesEngine>();
        services.AddScoped<IGoalService, GoalService>();

        // CashFlowForecastService implements both the API-facing and the
        // background-processing interfaces. Register the concrete type once.
        services.AddScoped<CashFlowForecastService>();
        services.AddScoped<ICashFlowForecastService>(
            sp => sp.GetRequiredService<CashFlowForecastService>());
        services.AddScoped<ICashFlowComputationService>(
            sp => sp.GetRequiredService<CashFlowForecastService>());

        // RecurringTransactionService implements both the API-facing and the
        // background-processing interfaces. Register the concrete type once.
        services.AddScoped<RecurringTransactionService>();
        services.AddScoped<IRecurringTransactionService>(
            sp => sp.GetRequiredService<RecurringTransactionService>());
        services.AddScoped<IRecurringTransactionEngineService>(
            sp => sp.GetRequiredService<RecurringTransactionService>());

        // BudgetService implements both the API-facing and the
        // background-processing interfaces. Register the concrete type once.
        services.AddScoped<BudgetService>();
        services.AddScoped<IBudgetService>(
            sp => sp.GetRequiredService<BudgetService>());
        services.AddScoped<IBudgetComputationService>(
            sp => sp.GetRequiredService<BudgetService>());

        services.AddScoped<ICalendarService, CalendarService>();
        services.AddScoped<IReceiptService, ReceiptService>();

        // ── Currency ──────────────────────────────────────────────────────────
        services.AddScoped<ICurrencyService, CurrencyService>();

        // ── Workspace ─────────────────────────────────────────────────────────
        services.AddScoped<IWorkspaceService, WorkspaceService>();

        return services;
    }
}
