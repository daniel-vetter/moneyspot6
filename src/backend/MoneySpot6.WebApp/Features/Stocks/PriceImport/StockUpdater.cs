using Microsoft.EntityFrameworkCore;
using MoneySpot6.WebApp.Database;
using MoneySpot6.WebApp.Features.Stocks.PriceImport.YahooAdapter;
using System;
using System.Collections.Immutable;

namespace MoneySpot6.WebApp.Features.Stocks.PriceImport;

[ScopedService]
public class StockUpdater
{
    private readonly Db _db;
    private readonly YahooStockDateProvider _yahooStockDateProvider;
    private readonly ILogger<StockUpdater> _logger;

    public StockUpdater(Db db, YahooStockDateProvider yahooStockDateProvider, ILogger<StockUpdater> logger)
    {
        _db = db;
        _yahooStockDateProvider = yahooStockDateProvider;
        _logger = logger;
    }

    public async Task Update(CancellationToken ct)
    {
        var allStocks = await _db
            .Stocks
            .AsTracking()
            .ToArrayAsync();

        foreach (var stock in allStocks)
        {
            ct.ThrowIfCancellationRequested();

            if (stock.Symbol == null)
                continue;

            Exception? error = null;
            try
            {
                var lastEntry = await _db.StockPrices
                    .Where(x => x.Stock == stock)
                    .OrderByDescending(x => x.Date)
                    .Take(1)
                    .SingleOrDefaultAsync();

                var start = lastEntry?.Date.AddDays(-10) ?? new DateOnly(2009, 1, 1);
                var end = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(1);

                var timeZone = TimeZoneInfo.Local; //TODO: Save to DB
                var entries = await _yahooStockDateProvider.Get(ToDateTimeOffsetUtc(start, timeZone), ToDateTimeOffsetUtc(end, timeZone), stock.Symbol, "1d");
                
                await Integrate(stock.Id, entries, timeZone);
            }
            catch (TaskCanceledException) { }
            catch (Exception e)
            {
                error = e;
            }

            stock.LastImport = DateTimeOffset.UtcNow;
            stock.LastImportError = error?.ToString();
            await _db.SaveChangesAsync();
        }
    }

    static DateTimeOffset ToDateTimeOffsetUtc(DateOnly dateOnly, TimeZoneInfo timeZone)
    {
        return new DateTimeOffset(dateOnly, TimeOnly.MinValue, timeZone.GetUtcOffset(dateOnly.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc))).ToUniversalTime();
    }

    static DateOnly ToDateOnly(DateTimeOffset dateTimeOffset, TimeZoneInfo timeZone)
    {
        var r = TimeZoneInfo.ConvertTime(dateTimeOffset, timeZone);
        return new DateOnly(r.Year, r.Month, r.Day);
    }

    private async Task Integrate(int stockId, ImmutableArray<StockPrice> stockPrices, TimeZoneInfo timeZone)
    {
        if (stockPrices.Length == 0)
            return;

        var stock = await _db
            .Stocks
            .AsTracking()
            .Where(x => x.Id == stockId)
            .SingleAsync();

        var min = stockPrices.Select(x => ToDateOnly(x.Timestamp, timeZone)).Min();
        var max = stockPrices.Select(x => ToDateOnly(x.Timestamp, timeZone)).Max();

        var existingEntries = await _db.StockPrices
            .AsTracking()
            .Where(x => x.Stock.Id == stockId)
            .Where(x => x.Date >= min && x.Date <= max)
            .ToDictionaryAsync(x => x.Date, x => x);

        var changedEntries = 0;
        var addedEntries = 0;
        foreach (var stockPrice in stockPrices)
        {
            if (existingEntries.TryGetValue(ToDateOnly(stockPrice.Timestamp, timeZone), out var existing))
            {
                if (existing.Open != stockPrice.Open ||
                    existing.Close != stockPrice.Close ||
                    existing.High != stockPrice.High ||
                    existing.Low != stockPrice.Low ||
                    existing.Volume != stockPrice.Volume)
                {
                    existing.Open = stockPrice.Open;
                    existing.Close = stockPrice.Close;
                    existing.High = stockPrice.High;
                    existing.Low = stockPrice.Low;
                    existing.Volume = stockPrice.Volume;
                    changedEntries++;
                }
            }
            else
            {
                _db.StockPrices.Add(new DbStockPrice
                {
                    Stock = stock,
                    Date = ToDateOnly(stockPrice.Timestamp, timeZone),
                    Open = stockPrice.Open,
                    Close = stockPrice.Close,
                    High = stockPrice.High,
                    Low = stockPrice.Low,
                    Volume = stockPrice.Volume,
                });
                addedEntries++;
            }
        }
        await _db.SaveChangesAsync();

        _logger.LogInformation("Integrated new prices for stock {name} ({id}). Retrieved: {retrieved}, Changed: {changed}, Added: {added}", stock.Name, stock.Id, stockPrices.Length, changedEntries, addedEntries);
    }
}