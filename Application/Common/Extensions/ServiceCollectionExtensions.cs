using Application.Features.Authentication.Services;
using Application.Features.Notifications;
using Application.Features.Notifications.Services;
using Application.Features.Onboarding.Services;
using Application.Interfaces.Services;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace Application.Common.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IOnboardingService, OnboardingService>();
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<INotificationPublisher, NotificationPublisher>();

        services.AddValidatorsFromAssemblyContaining(typeof(ServiceCollectionExtensions));

        return services;
    }
}
