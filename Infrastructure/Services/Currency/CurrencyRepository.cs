using Application.Features.Currency.DbModels;
using Application.Interfaces.Database;
using Application.Interfaces.Repositories;
using Dapper;
using System.Data;

namespace Infrastructure.Services.Currency;

internal sealed class CurrencyRepository(IDbExecutor db) : ICurrencyRepository
{
    private const string SP_Currency_GetList               = "MyMoney.usp_Currency_GetList";
    private const string SP_Currency_GetByCode             = "MyMoney.usp_Currency_GetByCode";
    private const string SP_UserCurrencyPrefs_Get          = "MyMoney.usp_UserCurrencyPreferences_Get";
    private const string SP_UserCurrencyPrefs_Upsert       = "MyMoney.usp_UserCurrencyPreferences_Upsert";
    private const string SP_ExchangeRate_GetCurrent        = "MyMoney.usp_ExchangeRate_GetCurrent";
    private const string SP_ExchangeRate_GetHistorical     = "MyMoney.usp_ExchangeRate_GetHistorical";
    private const string SP_ExchangeRate_GetList           = "MyMoney.usp_ExchangeRate_GetList";
    private const string SP_ExchangeRate_Upsert            = "MyMoney.usp_ExchangeRate_Upsert";
    private const string SP_ExchangeRate_BulkUpsert        = "MyMoney.usp_ExchangeRate_BulkUpsert";
    private const string SP_ExchangeRate_GetStalePairs     = "MyMoney.usp_ExchangeRate_GetStalePairs";
    private const string SP_ExchangeRate_GetStatistics     = "MyMoney.usp_ExchangeRate_GetStatistics";
    private const string SP_ExchangeRate_GetProviders      = "MyMoney.usp_ExchangeRate_GetActiveProviders";
    private const string SP_ConversionLog_Insert           = "MyMoney.usp_CurrencyConversionLog_Insert";
    private const string SP_Currency_GetDashboard          = "MyMoney.usp_Currency_GetDashboardSummary";

    // ── Currencies ────────────────────────────────────────────────────────────

    public Task<IReadOnlyList<CurrencyDbModel>> GetCurrenciesAsync(
        bool includeInactive = false, CancellationToken ct = default)
    {
        var p = new DynamicParameters();
        p.Add("@IncludeInactive", includeInactive ? 1 : 0, DbType.Byte);
        p.Add("@IncludeCrypto",   0,                       DbType.Byte);
        return db.QueryListAsync<CurrencyDbModel>(SP_Currency_GetList, p, ct);
    }

    public Task<CurrencyDbModel?> GetByCodeAsync(string code, CancellationToken ct = default)
    {
        var p = new DynamicParameters();
        p.Add("@Code", code, DbType.String, size: 10);
        return db.QuerySingleAsync<CurrencyDbModel>(SP_Currency_GetByCode, p, ct);
    }

    // ── User Preferences ──────────────────────────────────────────────────────

    public Task<UserCurrencyPreferencesDbModel> GetUserPreferencesAsync(
        long userId, CancellationToken ct = default)
    {
        var p = new DynamicParameters();
        p.Add("@UserId", userId, DbType.Int64);
        return db.QuerySingleAsync<UserCurrencyPreferencesDbModel>(SP_UserCurrencyPrefs_Get, p, ct)
            .ContinueWith(t => t.Result ?? new UserCurrencyPreferencesDbModel { UserId = userId });
    }

    public async Task<byte> UpsertUserPreferencesAsync(
        UpsertUserPreferencesDbModel model, CancellationToken ct = default)
    {
        var p = new DynamicParameters();
        p.Add("@UserId",              model.UserId,              DbType.Int64);
        p.Add("@BaseCurrencyCode",    model.BaseCurrencyCode,    DbType.String, size: 10);
        p.Add("@DisplayCurrencyCode", model.DisplayCurrencyCode, DbType.String, size: 10);
        p.Add("@NumberFormatId",      model.NumberFormatId,      DbType.Byte);
        p.Add("@SymbolStyleId",       model.SymbolStyleId,       DbType.Byte);
        p.Add("@NegativeFormatId",    model.NegativeFormatId,    DbType.Byte);
        p.Add("@CurrencyPositionId",  model.CurrencyPositionId,  DbType.Byte);
        p.Add("@ResultCode",          dbType: DbType.Byte, direction: ParameterDirection.Output);

        await db.ExecuteAsync(SP_UserCurrencyPrefs_Upsert, p, ct);
        return p.Get<byte>("@ResultCode");
    }

    // ── Exchange Rates ────────────────────────────────────────────────────────

    public Task<ExchangeRateDbModel?> GetCurrentRateAsync(
        string fromCurrency, string toCurrency, CancellationToken ct = default)
    {
        var p = new DynamicParameters();
        p.Add("@FromCurrency", fromCurrency, DbType.String, size: 10);
        p.Add("@ToCurrency",   toCurrency,   DbType.String, size: 10);
        return db.QuerySingleAsync<ExchangeRateDbModel>(SP_ExchangeRate_GetCurrent, p, ct);
    }

    public Task<ExchangeRateDbModel?> GetHistoricalRateAsync(
        string fromCurrency, string toCurrency, DateOnly asOfDate, CancellationToken ct = default)
    {
        var p = new DynamicParameters();
        p.Add("@FromCurrency", fromCurrency,          DbType.String, size: 10);
        p.Add("@ToCurrency",   toCurrency,            DbType.String, size: 10);
        p.Add("@AsOfDate",     asOfDate.ToString("yyyy-MM-dd"), DbType.Date);
        return db.QuerySingleAsync<ExchangeRateDbModel>(SP_ExchangeRate_GetHistorical, p, ct);
    }

    public async Task<(IReadOnlyList<ExchangeRateDbModel> Items, int TotalCount)> GetRateListAsync(
        GetRateListDbModel model, CancellationToken ct = default)
    {
        var p = new DynamicParameters();
        p.Add("@FromCurrency", model.FromCurrency, DbType.String, size: 10);
        p.Add("@ToCurrency",   model.ToCurrency,   DbType.String, size: 10);
        p.Add("@StatusId",     model.StatusId,     DbType.Byte);
        p.Add("@DateFrom",     model.DateFrom?.ToString("yyyy-MM-dd"), DbType.Date);
        p.Add("@DateTo",       model.DateTo?.ToString("yyyy-MM-dd"),   DbType.Date);
        p.Add("@PageNumber",   model.PageNumber,   DbType.Int32);
        p.Add("@PageSize",     model.PageSize,     DbType.Int32);

        var items = await db.QueryListAsync<ExchangeRateDbModel>(SP_ExchangeRate_GetList, p, ct);
        var total = items.Count > 0 ? items[0].TotalCount ?? items.Count : 0;
        return (items, total);
    }

    public async Task<(long RateId, byte ResultCode)> UpsertRateAsync(
        UpsertExchangeRateDbModel model, CancellationToken ct = default)
    {
        var p = new DynamicParameters();
        p.Add("@FromCurrency",  model.FromCurrency,                        DbType.String, size: 10);
        p.Add("@ToCurrency",    model.ToCurrency,                          DbType.String, size: 10);
        p.Add("@Rate",          model.Rate,                                DbType.Decimal, precision: 28, scale: 10);
        p.Add("@ProviderId",    model.ProviderId,                          DbType.Int32);
        p.Add("@EffectiveDate", model.EffectiveDate.ToString("yyyy-MM-dd"),DbType.Date);
        p.Add("@SourceTypeId",  model.SourceTypeId,                        DbType.Byte);
        p.Add("@CreatedBy",     model.CreatedBy,                           DbType.Int64);
        p.Add("@NewRateId",     dbType: DbType.Int64, direction: ParameterDirection.Output);
        p.Add("@ResultCode",    dbType: DbType.Byte,  direction: ParameterDirection.Output);

        await db.ExecuteAsync(SP_ExchangeRate_Upsert, p, ct);
        return (p.Get<long>("@NewRateId"), p.Get<byte>("@ResultCode"));
    }

    public async Task<(int InsertedCount, int ArchivedCount)> BulkUpsertRatesAsync(
        int providerId, string ratesJson, byte sourceTypeId = 2, CancellationToken ct = default)
    {
        var p = new DynamicParameters();
        p.Add("@RatesJson",     ratesJson,    DbType.String);
        p.Add("@ProviderId",    providerId,   DbType.Int32);
        p.Add("@SourceTypeId",  sourceTypeId, DbType.Byte);
        p.Add("@InsertedCount", dbType: DbType.Int32, direction: ParameterDirection.Output);
        p.Add("@ArchivedCount", dbType: DbType.Int32, direction: ParameterDirection.Output);

        await db.ExecuteAsync(SP_ExchangeRate_BulkUpsert, p, ct);
        return (p.Get<int>("@InsertedCount"), p.Get<int>("@ArchivedCount"));
    }

    public Task<IReadOnlyList<StaleRatePairDbModel>> GetStalePairsAsync(
        int staleDays = 2, CancellationToken ct = default)
    {
        var p = new DynamicParameters();
        p.Add("@StaleDays", staleDays, DbType.Int32);
        return db.QueryListAsync<StaleRatePairDbModel>(SP_ExchangeRate_GetStalePairs, p, ct);
    }

    public Task<(ExchangeRateStatsSummaryDbModel Summary, IReadOnlyList<ProviderStatDbModel> Providers)>
        GetStatisticsAsync(CancellationToken ct = default)
    {
        return db.QueryMultipleAsync(
            SP_ExchangeRate_GetStatistics,
            async reader =>
            {
                var summary   = await reader.ReadFirstOrDefaultAsync<ExchangeRateStatsSummaryDbModel>()
                    ?? new ExchangeRateStatsSummaryDbModel();
                var providers = (await reader.ReadAsync<ProviderStatDbModel>()).AsList();
                return (Summary: summary, Providers: (IReadOnlyList<ProviderStatDbModel>)providers);
            },
            ct: ct);
    }

    // ── Providers ─────────────────────────────────────────────────────────────

    public Task<IReadOnlyList<ExchangeRateProviderDbModel>> GetActiveProvidersAsync(
        CancellationToken ct = default)
        => db.QueryListAsync<ExchangeRateProviderDbModel>(SP_ExchangeRate_GetProviders, ct: ct);

    // ── Conversion Log ────────────────────────────────────────────────────────

    public async Task<long> LogConversionAsync(
        LogConversionDbModel model, CancellationToken ct = default)
    {
        var p = new DynamicParameters();
        p.Add("@UserId",             model.UserId,                                DbType.Int64);
        p.Add("@EntityType",         model.EntityType,                            DbType.String, size: 50);
        p.Add("@EntityId",           model.EntityId,                              DbType.Int64);
        p.Add("@FromCurrency",       model.FromCurrency,                          DbType.String, size: 10);
        p.Add("@ToCurrency",         model.ToCurrency,                            DbType.String, size: 10);
        p.Add("@OriginalAmount",     model.OriginalAmount,                        DbType.Decimal, precision: 18, scale: 4);
        p.Add("@ConvertedAmount",    model.ConvertedAmount,                       DbType.Decimal, precision: 18, scale: 4);
        p.Add("@ExchangeRate",       model.ExchangeRate,                          DbType.Decimal, precision: 28, scale: 10);
        p.Add("@RateId",             model.RateId,                               DbType.Int64);
        p.Add("@RateEffectiveDate",  model.RateEffectiveDate.ToString("yyyy-MM-dd"), DbType.Date);
        p.Add("@ConversionModeId",   model.ConversionModeId,                     DbType.Byte);
        p.Add("@LogId",              dbType: DbType.Int64, direction: ParameterDirection.Output);

        await db.ExecuteAsync(SP_ConversionLog_Insert, p, ct);
        return p.Get<long>("@LogId");
    }

    // ── Dashboard ─────────────────────────────────────────────────────────────

    public Task<(CurrencyDashboardSummaryDbModel Summary, IReadOnlyList<CurrencyBreakdownDbModel> Breakdown)>
        GetDashboardSummaryAsync(long userId, string displayCurrency,
            DateOnly? dateFrom, DateOnly? dateTo, CancellationToken ct = default)
    {
        var p = new DynamicParameters();
        p.Add("@UserId",          userId,                            DbType.Int64);
        p.Add("@DisplayCurrency", displayCurrency,                  DbType.String, size: 10);
        p.Add("@DateFrom",        dateFrom?.ToString("yyyy-MM-dd"), DbType.Date);
        p.Add("@DateTo",          dateTo?.ToString("yyyy-MM-dd"),   DbType.Date);

        return db.QueryMultipleAsync(
            SP_Currency_GetDashboard,
            async reader =>
            {
                var summary   = await reader.ReadFirstOrDefaultAsync<CurrencyDashboardSummaryDbModel>()
                    ?? new CurrencyDashboardSummaryDbModel();
                var breakdown = (await reader.ReadAsync<CurrencyBreakdownDbModel>()).AsList();
                return (Summary: summary, Breakdown: (IReadOnlyList<CurrencyBreakdownDbModel>)breakdown);
            },
            p, ct);
    }
}
