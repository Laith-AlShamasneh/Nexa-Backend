using Application.Interfaces.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Jobs;

internal sealed class NotificationCleanupService(
    IServiceScopeFactory    scopeFactory,
    ILogger<NotificationCleanupService> logger) : BackgroundService
{
    private const int RetentionDays   = 180;
    private static readonly TimeSpan  Interval = TimeSpan.FromHours(24);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunCleanupAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Notification cleanup cycle failed.");
            }

            try
            {
                await Task.Delay(Interval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    private async Task RunCleanupAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var repo        = scope.ServiceProvider.GetRequiredService<INotificationRepository>();
        var deleted     = await repo.CleanupExpiredAsync(RetentionDays, ct);

        if (deleted > 0)
            logger.LogInformation("Notification cleanup deleted {Count} expired notifications.", deleted);
    }
}
