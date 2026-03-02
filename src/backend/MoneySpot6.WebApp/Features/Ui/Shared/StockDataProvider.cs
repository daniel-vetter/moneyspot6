using System.Collections.Immutable;
using Microsoft.EntityFrameworkCore;
using MoneySpot6.WebApp.Common;
using MoneySpot6.WebApp.Database;
using MoneySpot6.WebApp.Infrastructure;

namespace MoneySpot6.WebApp.Features.Ui.Shared;

[ScopedService]
public class StockDataProvider
{
    private readonly Db _db;

    public StockDataProvider(Db db)
    {
        _db = db;
    }

    /// <summary>
    /// Returns the price of a single stock over a time range.
    /// Result contains an entry for each day.
    /// </summary>
    public async Task<ImmutableTimeline<StartAndEnd<decimal>>> GetDailyStockPrice(int stockId, DateOnly start, DateOnly end)
    {
        var startDto = new DateTimeOffset(start.Year, start.Month, start.Day, 0, 0, 0, 0, 0, TimeSpan.Zero);
        var endDto = new DateTimeOffset(end.Year, end.Month, end.Day, 0, 0, 0, 0, 0, TimeSpan.Zero);

        var lastBeforeStart = await _db.StockPrices
            .AsNoTracking()
            .Where(x => x.Stock.Id == stockId)
            .Where(x => x.Interval == StockPriceInterval.Daily)
            .Where(x => x.Timestamp < startDto)
            .OrderByDescending(x => x.Timestamp)
            .FirstOrDefaultAsync();

        var startValue = lastBeforeStart == null
            ? new StartAndEnd<decimal>(0, 0)
            : new StartAndEnd<decimal>(lastBeforeStart.Close, lastBeforeStart.Close);

        var entries = await _db.StockPrices
            .AsNoTracking()
            .Where(x => x.Stock.Id == stockId)
            .Where(x => x.Interval == StockPriceInterval.Daily)
            .Where(x => x.Timestamp >= startDto && x.Timestamp < endDto)
            .ToImmutableArrayAsync();

        var entriesByDay = entries
            .GroupBy(x => DateOnly.FromDateTime(x.Timestamp.DateTime))
            .ToDictionary(x => x.Key, x => new StartAndEnd<decimal>(x.First().Open, x.First().Close));

        return ImmutableTimeline.CreateContinuous(start, end, startValue, entriesByDay);
    }

    /// <summary>
    /// Returns the amount and price (when bought) of a stock over a time range.
    /// Result contains an entry for each day.
    /// </summary>
    public async Task<ImmutableTimeline<StartAndEnd<StockOwnership>>> GetDailyStockOwnership(int stockId, DateOnly start, DateOnly end)
    {
        var startDto = new DateOnly(start.Year, start.Month, start.Day);
        var endDto = new DateOnly(end.Year, end.Month, end.Day);

        var startValues = await _db.StockTransactions
            .AsNoTracking()
            .Where(x => x.Stock.Id == stockId)
            .Where(x => x.Date < startDto)
            .GroupBy(x => true)
            .Select(x => new
            {
                AmountSum = x.Sum(y => y.Amount),
                PriceSum = x.Sum(y => y.Price * y.Amount)
            })
            .SingleOrDefaultAsync();

        var stockTransactions = (await _db.StockTransactions
                .AsNoTracking()
                .Where(x => x.Stock.Id == stockId)
                .Where(x => x.Date >= startDto && x.Date < endDto)
                .ToImmutableArrayAsync())
            .GroupBy(x => x.Date)
            .OrderBy(x => x.Key)
            .ToImmutableArray()
            .ToImmutableDictionary(x => x.Key, x => new
            {
                Amount = x.Sum(y => y.Amount),
                Price = x.Sum(y => y.Price * y.Amount)
            });

        var r = new StartAndEnd<StockOwnership>[end.DayNumber - start.DayNumber];
        var curAmount = startValues?.AmountSum ?? 0;
        var curPrice = startValues?.PriceSum ?? 0;
        for (var cur = start; cur < end; cur = cur.AddDays(1))
        {
            var before = new StockOwnership(curAmount, curPrice);
            if (stockTransactions.TryGetValue(cur, out var stockTransaction))
            {
                curAmount += stockTransaction.Amount;
                curPrice += stockTransaction.Price;
            }
            var after = new StockOwnership(curAmount, curPrice);

            r[cur.DayNumber - start.DayNumber] = new StartAndEnd<StockOwnership>(before, after);
        }
        return ImmutableTimeline.Create(start, end, r);
    }

    /// <summary>
    /// Returns the value of owned stock over a time range.
    /// Result contains an entry for each day (end of day).
    /// </summary>
    public async Task<ImmutableTimeline<StartAndEnd<OwnedStockValue>>> GetDailyOwnedStockValue(DateOnly start, DateOnly end, ImmutableArray<int>? stockIds = null)
    {
        stockIds ??= await _db.Stocks.Select(x => x.Id).ToImmutableArrayAsync();
        var r = new StartAndEnd<OwnedStockValue>[end.DayNumber - start.DayNumber];
        for (var i = 0;i < r.Length;i++)
        {
            r[i] = new StartAndEnd<OwnedStockValue>(new OwnedStockValue(0, 0), new OwnedStockValue(0, 0));
        }

        foreach (var stockId in stockIds)
        {
            var stockValueTimeline = await GetDailyStockPrice(stockId, start, end);
            var ownedStockTimeline = await GetDailyStockOwnership(stockId, start, end);

            for (var date = start; date < end; date = date.AddDays(1))
            {
                var ownedStock = ownedStockTimeline[date];
                var stockValue = stockValueTimeline[date];

                var existing = r[date.DayNumber - start.DayNumber];
                r[date.DayNumber - start.DayNumber] = new StartAndEnd<OwnedStockValue>(
                    new OwnedStockValue(
                        existing.StartOfDay.CurrentValue + ownedStock.StartOfDay.Amount * stockValue.StartOfDay,
                        existing.StartOfDay.InvestedValue + ownedStock.StartOfDay.PriceBoughtFor
                    ),
                    new OwnedStockValue(
                        existing.EndOfDay.CurrentValue + ownedStock.EndOfDay.Amount * stockValue.EndOfDay,
                        existing.EndOfDay.InvestedValue + ownedStock.EndOfDay.PriceBoughtFor
                    )
                );
            }
        }
        return ImmutableTimeline.Create(start, end, r);
    }

    /// <summary>
    /// Returns the current price of all stocks
    /// </summary>
    public async Task<ImmutableDictionary<int, decimal>> GetStockPrices()
    {
        return await _db.StockPrices
            .AsNoTracking()
            .Where(x => x.Interval == StockPriceInterval.FiveMinutes)
            .GroupBy(x => x.Stock.Id)
            .Select(x => new
            {
                StockId = x.Key,
                Price = x.OrderByDescending(y => y.Timestamp).First()
            })
            .ToImmutableDictionaryAsync(x => x.StockId, x => x.Price.Close);
    }
}

public record StartAndEnd<T>(T StartOfDay, T EndOfDay);
public record StockOwnership(decimal Amount, decimal PriceBoughtFor);
public record OwnedStockValue(decimal CurrentValue, decimal InvestedValue);