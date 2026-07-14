using Application.Interfaces.Services;

namespace Infrastructure.Services.Currency;

/// <summary>
/// Default provider: returns rates already stored in the database.
/// Used when no external API provider is configured or when the system
/// operates in manual-rate mode. Never makes external HTTP calls.
/// </summary>
internal sealed class ManualExchangeRateProvider : IExchangeRateProvider
{
    public string ProviderCode => "MANUAL";

    // Manual provider cannot fetch new rates — this should never be called.
    public Task<IReadOnlyList<ExchangeRateData>> GetLatestRatesAsync(
        string baseCurrency, CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<ExchangeRateData>>(Array.Empty<ExchangeRateData>());

    public bool SupportsPair(string fromCurrency, string toCurrency) => false;
}
