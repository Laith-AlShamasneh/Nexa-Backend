using Application.Common.Constants;
using Application.Features.Currency.Jobs;
using Application.Interfaces.Jobs;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Infrastructure.Jobs.Handlers;

/// <summary>
/// Handles the ExchangeRateSync job.
/// Delegates to the registered IExchangeRateProvider for the given ProviderCode.
/// Falls back gracefully if the provider returns no rates.
/// </summary>
internal sealed class ExchangeRateSyncHandler(
    IEnumerable<IExchangeRateProvider> providers,
    ICurrencyRepository                currencyRepository,
    ICacheService                      cacheService,
    ILogger<ExchangeRateSyncHandler>   logger) : IJobHandler
{
    public string JobType => JobTypes.ExchangeRateSync;

    public async Task HandleAsync(string payload, CancellationToken ct)
    {
        var job = JsonSerializer.Deserialize<ExchangeRateSyncPayload>(payload)
            ?? throw new InvalidOperationException("Invalid ExchangeRateSync payload.");

        logger.LogInformation(
            "ExchangeRateSync starting. Provider={ProviderCode} Base={Base} Manual={Manual}",
            job.ProviderCode, job.BaseCurrency, job.IsManualTrigger);

        // Find the provider implementation
        var provider = providers.FirstOrDefault(p =>
            p.ProviderCode.Equals(job.ProviderCode, StringComparison.OrdinalIgnoreCase));

        if (provider is null)
        {
            logger.LogWarning("ExchangeRateSync: provider '{ProviderCode}' not found — skipping.", job.ProviderCode);
            return;
        }

        // Fetch rates from the provider
        var rates = await provider.GetLatestRatesAsync(job.BaseCurrency, ct);

        if (rates.Count == 0)
        {
            logger.LogWarning("ExchangeRateSync: provider '{ProviderCode}' returned 0 rates.", job.ProviderCode);
            return;
        }

        // Filter to requested target currencies (empty = all)
        if (job.TargetCurrencies.Length > 0)
        {
            var targetSet = new HashSet<string>(job.TargetCurrencies, StringComparer.OrdinalIgnoreCase);
            rates = rates.Where(r => targetSet.Contains(r.ToCurrency)).ToList();
        }

        // Load provider database record
        var dbProviders = await currencyRepository.GetActiveProvidersAsync(ct);
        var dbProvider  = dbProviders.FirstOrDefault(p =>
            p.Code.Equals(job.ProviderCode, StringComparison.OrdinalIgnoreCase));

        if (dbProvider is null)
        {
            logger.LogWarning("ExchangeRateSync: provider '{ProviderCode}' not in DB.", job.ProviderCode);
            return;
        }

        // Serialize to JSON for bulk upsert
        var ratesJson = JsonSerializer.Serialize(rates.Select(r => new
        {
            r.FromCurrency,
            r.ToCurrency,
            r.Rate,
            EffectiveDate = r.EffectiveDate.ToString("yyyy-MM-dd"),
        }));

        var (inserted, archived) = await currencyRepository.BulkUpsertRatesAsync(
            dbProvider.ProviderId, ratesJson, sourceTypeId: 2, ct);

        // Invalidate current-rate cache for all synced pairs
        foreach (var rate in rates)
        {
            await cacheService.RemoveAsync($"exrate:current:{rate.FromCurrency}:{rate.ToCurrency}");
            await cacheService.RemoveAsync($"exrate:current:{rate.ToCurrency}:{rate.FromCurrency}");
        }

        logger.LogInformation(
            "ExchangeRateSync complete. Inserted={Inserted} Archived={Archived}",
            inserted, archived);
    }
}
