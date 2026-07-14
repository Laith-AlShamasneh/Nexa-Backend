using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Shared.Enums.Finance;

namespace Infrastructure.Services.Currency;

/// <summary>
/// Centralized conversion engine. All monetary conversions in the system
/// flow through this class — never compute rates inline elsewhere.
///
/// Precision rules:
///   • Internal calculations use decimal (never double/float).
///   • Rates stored with 10 decimal places; converted amounts rounded to currency's decimal digits.
///   • Banker's rounding (MidpointRounding.ToEven) for financial accuracy.
/// </summary>
internal sealed class CurrencyConversionService(
    ICurrencyRepository currencyRepository,
    ICacheService       cacheService) : ICurrencyConversionService
{
    private const string RateCacheKeyPrefix = "exrate:current:";
    private static readonly TimeSpan RateCacheTtl = TimeSpan.FromHours(1);

    // ── Public API ────────────────────────────────────────────────────────────

    public async Task<ConversionResult> ConvertAsync(
        decimal amount, string fromCurrency, string toCurrency, CancellationToken ct = default)
    {
        if (fromCurrency == toCurrency)
            return IdentityResult(amount, fromCurrency);

        var rate = await GetCurrentRateAsync(fromCurrency, toCurrency, ct);
        if (rate is null)
            return FailedResult(amount, fromCurrency, toCurrency);

        return BuildResult(amount, rate);
    }

    public async Task<ConversionResult> ConvertHistoricalAsync(
        decimal amount, string fromCurrency, string toCurrency,
        DateOnly asOfDate, CancellationToken ct = default)
    {
        if (fromCurrency == toCurrency)
            return IdentityResult(amount, fromCurrency);

        var rate = await GetHistoricalRateAsync(fromCurrency, toCurrency, asOfDate, ct);
        if (rate is null)
            return FailedResult(amount, fromCurrency, toCurrency);

        return BuildResult(amount, rate);
    }

    public async Task<IReadOnlyList<ConversionResult>> ConvertBatchAsync(
        IReadOnlyList<ConversionRequest> requests,
        string targetCurrency,
        CancellationToken ct = default)
    {
        // Collect distinct source currencies (excluding identity)
        var sourceCurrencies = requests
            .Select(r => r.FromCurrency)
            .Distinct()
            .Where(c => c != targetCurrency)
            .ToList();

        // Resolve all rates in parallel
        var rates = new Dictionary<string, RateSnapshot?>(StringComparer.OrdinalIgnoreCase);
        var rateTasks = sourceCurrencies
            .Select(async from =>
            {
                var rate = await GetCurrentRateAsync(from, targetCurrency, ct);
                return (from, rate);
            });

        foreach (var (from, rate) in await Task.WhenAll(rateTasks))
            rates[from] = rate;

        return requests
            .Select(req =>
            {
                if (req.FromCurrency == targetCurrency)
                    return IdentityResult(req.Amount, req.FromCurrency);

                var rate = rates.GetValueOrDefault(req.FromCurrency);
                return rate is null
                    ? FailedResult(req.Amount, req.FromCurrency, targetCurrency)
                    : BuildResult(req.Amount, rate);
            })
            .ToList();
    }

    public async Task<RateSnapshot?> GetCurrentRateAsync(
        string fromCurrency, string toCurrency, CancellationToken ct = default)
    {
        if (fromCurrency == toCurrency)
            return IdentityRateSnapshot(fromCurrency);

        // Check cache first
        var cacheKey = $"{RateCacheKeyPrefix}{fromCurrency}:{toCurrency}";
        var cached = await cacheService.GetAsync<RateSnapshot>(cacheKey);
        if (cached is not null)
            return cached;

        var db = await currencyRepository.GetCurrentRateAsync(fromCurrency, toCurrency, ct);
        if (db is null)
            return null;

        var snapshot = new RateSnapshot(
            db.FromCurrency,
            db.ToCurrency,
            db.Rate,
            db.InverseRate,
            db.EffectiveDate,
            db.RateId,
            (ExchangeRateSourceType)db.SourceTypeId);

        await cacheService.SetAsync(cacheKey, snapshot, RateCacheTtl);
        return snapshot;
    }

    public async Task<RateSnapshot?> GetHistoricalRateAsync(
        string fromCurrency, string toCurrency, DateOnly asOfDate, CancellationToken ct = default)
    {
        if (fromCurrency == toCurrency)
            return IdentityRateSnapshot(fromCurrency);

        // Historical rates are never cached (immutable but unbounded key space)
        var db = await currencyRepository.GetHistoricalRateAsync(fromCurrency, toCurrency, asOfDate, ct);
        if (db is null)
            return null;

        return new RateSnapshot(
            db.FromCurrency,
            db.ToCurrency,
            db.Rate,
            db.InverseRate,
            db.EffectiveDate,
            db.RateId,
            (ExchangeRateSourceType)db.SourceTypeId);
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private static ConversionResult BuildResult(decimal amount, RateSnapshot rate)
    {
        // Apply rate with 10-decimal precision, then round to 4 decimal places
        // (caller rounds further to currency-specific decimal digits for display)
        var converted = Math.Round(amount * rate.Rate, 4, MidpointRounding.ToEven);

        return new ConversionResult(
            OriginalAmount:      amount,
            FromCurrency:        rate.FromCurrency,
            ConvertedAmount:     converted,
            ToCurrency:          rate.ToCurrency,
            ExchangeRate:        rate.Rate,
            RateEffectiveDate:   rate.EffectiveDate,
            RateId:              rate.RateId,
            SourceType:          rate.SourceType,
            IsIdentityConversion: false);
    }

    private static ConversionResult IdentityResult(decimal amount, string currency) =>
        new(amount, currency, amount, currency,
            1m, DateOnly.FromDateTime(DateTime.UtcNow), null,
            ExchangeRateSourceType.Manual, IsIdentityConversion: true);

    // No rate available: ConvertedAmount/ExchangeRate are 0 — callers must check Succeeded
    // and treat the result as a failure rather than a real zero-valued conversion.
    private static ConversionResult FailedResult(decimal amount, string from, string to) =>
        new(amount, from, 0m, to,
            0m, DateOnly.FromDateTime(DateTime.UtcNow), null,
            ExchangeRateSourceType.Manual, IsIdentityConversion: false, Succeeded: false);

    private static RateSnapshot IdentityRateSnapshot(string currency) =>
        new(currency, currency, 1m, 1m,
            DateOnly.FromDateTime(DateTime.UtcNow), null,
            ExchangeRateSourceType.Manual);
}
