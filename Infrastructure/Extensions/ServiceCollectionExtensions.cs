using Application.Common.Options;
using Application.Interfaces.Jobs;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Infrastructure.Database;
using Infrastructure.Jobs;
using Infrastructure.Jobs.Handlers;
using Infrastructure.Jobs.Options;
using Infrastructure.Services;
using Infrastructure.Services.Authentication;
using Infrastructure.Services.Authentication.Options;
using Infrastructure.Services.Caching;
using Infrastructure.Services.Email;
using Infrastructure.Services.Email.Options;
using Infrastructure.Services.Localization;
using Infrastructure.Services.Notifications;
using Infrastructure.Services.Onboarding;
using Infrastructure.Services.Storage;
using Infrastructure.Services.Storage.Options;
using Infrastructure.Services.Tenancy;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHttpContextAccessor();
        services.AddMemoryCache();

        services.Configure<AuthenticationOptions>(configuration.GetSection("Authentication"));
        services.Configure<JwtOptions>(configuration.GetSection("Jwt"));
        services.Configure<SmtpOptions>(configuration.GetSection("Smtp"));
        services.Configure<StorageOptions>(configuration.GetSection("Storage"));
        services.Configure<BackgroundJobOptions>(configuration.GetSection("BackgroundJobs"));

        // ── Database ─────────────────────────────────────────────────────────
        services.AddScoped<ISqlConnectionFactory, SqlConnectionFactory>();
        services.AddScoped<IDbExecutor, DbExecutor>();

        // ── Authentication ───────────────────────────────────────────────────
        services.AddScoped<IAuthRepository, AuthRepository>();
        services.AddScoped<IPasswordHasher, PasswordHasher>();
        services.AddScoped<ITokenHasher, TokenHasher>();
        services.AddScoped<IJwtService, JwtService>();
        services.AddScoped<IUserContext, UserContext>();

        // ── Caching / Localization ───────────────────────────────────────────
        services.AddSingleton<ICacheService, MemoryCacheService>();
        services.AddScoped<IMessageProvider, MessageProvider>();

        // ── Email ─────────────────────────────────────────────────────────────
        services.AddScoped<IEmailService, SmtpEmailService>();
        services.AddScoped<IEmailTemplateService, EmailTemplateService>();

        // ── Storage ───────────────────────────────────────────────────────────
        services.AddScoped<IFileService, LocalFileService>();
        services.AddScoped<IFileLinkService, FileLinkService>();
        services.AddScoped<IStorageUtility, StorageUtility>();

        // ── Notifications ────────────────────────────────────────────────────
        services.AddScoped<INotificationRepository, NotificationRepository>();
        services.AddHostedService<NotificationCleanupService>();

        // ── Onboarding ────────────────────────────────────────────────────────
        services.AddScoped<IOnboardingRepository, OnboardingRepository>();

        // ── Tenancy (tenant onboarding / organization registration) ──────────
        services.AddScoped<IOrganizationRegistrationRepository, OrganizationRegistrationRepository>();
        services.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>();

        // ── Background jobs ──────────────────────────────────────────────────
        services.AddScoped<IBackgroundJobRepository, BackgroundJobRepository>();
        services.AddScoped<IBackgroundJobService, BackgroundJobService>();
        services.AddHostedService<BackgroundJobProcessor>();

        // ── Scheduled (recurring) jobs ───────────────────────────────────────
        services.AddScoped<IScheduledJobRepository, ScheduledJobRepository>();
        if (configuration.GetSection("BackgroundJobs").Get<BackgroundJobOptions>()?.RunSchedulers ?? true)
            services.AddHostedService<ScheduledJobProcessor>();

        services.AddScoped<IJobHandler, WelcomeEmailHandler>();
        services.AddScoped<IJobHandler, EmailConfirmationHandler>();
        services.AddScoped<IJobHandler, PasswordResetEmailHandler>();
        services.AddScoped<IJobHandler, PasswordChangedEmailHandler>();
        services.AddScoped<IJobHandler, EmailChangeRequestedHandler>();
        services.AddScoped<IJobHandler, EmailChangedHandler>();
        services.AddScoped<IJobHandler, OrganizationInvitationEmailHandler>();
        services.AddScoped<IJobHandler, CreateNotificationHandler>();

        return services;
    }
}
