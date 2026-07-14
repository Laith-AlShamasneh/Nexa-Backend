using Application.Common.Constants;
using Application.Features.Currency.Jobs;
using Application.Interfaces.Jobs;
using Application.Interfaces.Repositories;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Infrastructure.Jobs.Handlers;

/// <summary>
/// Scans for stale or missing exchange rates and emits log warnings.
/// Runs daily. Future enhancement: enqueue a SyncJob automatically when
/// stale pairs are detected.
/// </summary>
internal sealed class ExchangeRateValidationHandler(
    ICurrencyRepository                        currencyRepository,
    ILogger<ExchangeRateValidationHandler>     logger) : IJobHandler
{
    public string JobType => JobTypes.ExchangeRateValidation;

    public async Task HandleAsync(string payload, CancellationToken ct)
    {
        var job = JsonSerializer.Deserialize<ExchangeRateValidationPayload>(payload)
            ?? new ExchangeRateValidationPayload();

        logger.LogInformation("ExchangeRateValidation starting. StaleDays={Days}", job.StaleDaysThreshold);

        var stalePairs = await currencyRepository.GetStalePairsAsync(job.StaleDaysThreshold, ct);

        if (stalePairs.Count == 0)
        {
            logger.LogInformation("ExchangeRateValidation: all rates are current.");
            return;
        }

        foreach (var pair in stalePairs)
        {
            logger.LogWarning(
                "Stale exchange rate: {From}→{To} last updated {Days} day(s) ago (last: {Date})",
                pair.FromCurrency, pair.ToCurrency, pair.DaysSinceUpdate,
                pair.LastRateDate.ToString("yyyy-MM-dd"));
        }

        logger.LogWarning("ExchangeRateValidation: {Count} stale rate pair(s) detected.", stalePairs.Count);
    }
}
