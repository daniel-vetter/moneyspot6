using System.Collections.Immutable;
using Microsoft.EntityFrameworkCore;
using MoneySpot6.WebApp.Database;
using MoneySpot6.WebApp.Infrastructure;

namespace MoneySpot6.WebApp.Features.Shared;

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
    public async Task<ImmutableArray<StockValue>> GetDailyStockValue(int stockId, DateOnly start, DateOnly end)
    {
        var startDto = new DateTimeOffset(start.Year, start.Month, start.Day, 0, 0, 0, 0, 0, TimeSpan.Zero);
        var endDto = new DateTimeOffset(end.Year, end.Month, end.Day, 0, 0, 0, 0, 0, TimeSpan.Zero);

        var entries = await _db.StockPrices
            .AsNoTracking()
            .Where(x => x.Stock.Id == stockId)
            .Where(x => x.Interval == StockPriceInterval.Daily)
            .Where(x => x.Timestamp >= startDto && x.Timestamp < endDto)
            .ToImmutableArrayAsync();

        var entriesByDay = entries
            .GroupBy(x => DateOnly.FromDateTime(x.Timestamp.DateTime))
            .ToDictionary(x => x.Key, x => x.ToImmutableArray());

        var result = ImmutableArray.CreateBuilder<StockValue>();
        for (var curDay = start; curDay < end; curDay = curDay.AddDays(1))
        {
            if (entriesByDay.TryGetValue(curDay, out var entriesOfCurDay))
            {
                var entry = entriesOfCurDay
                    .OrderByDescending(x => x.Timestamp)
                    .First();

                result.Add(new StockValue(curDay, entry.Close));
            }
            else
            {
                if (result.Count == 0)
                {
                    var lastBeforeStart = await _db.StockPrices
                        .AsNoTracking()
                        .Where(x => x.Stock.Id == stockId)
                        .Where(x => x.Interval == StockPriceInterval.Daily)
                        .Where(x => x.Timestamp < startDto)
                        .OrderByDescending(x => x.Timestamp)
                        .FirstOrDefaultAsync();

                    if (lastBeforeStart == null)
                    {
                        result.Add(new(curDay, 0m));
                    }
                    else
                    {
                        result.Add(new(curDay, lastBeforeStart.Close));
                    }
                }
                else
                {
                    result.Add(new StockValue(curDay, result[^1].Value));
                }
            }
        }

        return result.ToImmutable();
    }

    /// <summary>
    /// Returns the amount of owned shares of a stock over a time range.
    /// Result contains an entry for each day.
    /// </summary>
    public async Task<ImmutableArray<OwnedStockAmount>> GetDailyOwnedStockAmount(int stockId, DateOnly start, DateOnly end)
    {
        var startDto = new DateOnly(start.Year, start.Month, start.Day);
        var endDto = new DateOnly(end.Year, end.Month, end.Day);

        var curAmount = await _db.StockTransactions
            .AsNoTracking()
            .Where(x => x.Stock.Id == stockId)
            .Where(x => x.Date < startDto)
            .SumAsync(x => x.Amount);

        var stockTransactions = (await _db.StockTransactions
                .AsNoTracking()
                .Where(x => x.Stock.Id == stockId)
                .Where(x => x.Date >= startDto && x.Date < endDto)
                .ToImmutableArrayAsync())
            .GroupBy(x => x.Date)
            .ToImmutableDictionary(x => x.Key, x => x.Sum(y => y.Amount));

        var result = ImmutableArray.CreateBuilder<OwnedStockAmount>();
        for (var curDate = start; curDate < end; curDate = curDate.AddDays(1))
        {
            if (stockTransactions.TryGetValue(curDate, out var changeAtDay)) 
                curAmount += changeAtDay;

            result.Add(new OwnedStockAmount(curDate, curAmount));
        }
        return result.ToImmutable();
    }

    /// <summary>
    /// Returns the value of owned stock over a time range.
    /// /// Result contains an entry for each day.
    /// </summary>
    public async Task<ImmutableArray<OwnedStockValue>> GetDailyOwnedStockValue(DateOnly start, DateOnly end, ImmutableArray<int>? stockIds = null)
    {
        stockIds ??= await _db.Stocks.Select(x => x.Id).ToImmutableArrayAsync();

        var result = ImmutableArray.CreateBuilder<OwnedStockValue>();
        for (var curDay = start; curDay < end; curDay = curDay.AddDays(1))
            result.Add(new OwnedStockValue(curDay, 0));

        foreach (var stockId in stockIds)
        {
            var amount = await GetDailyOwnedStockAmount(stockId, start, end);
            var value = await GetDailyStockValue(stockId, start, end);

            if (amount.Length != result.Count || value.Length != result.Count)
                throw new Exception("Length of lists is not equal.");

            for (var i = 0; i < amount.Length; i++)
            {
                result[i] = result[i] with
                {
                    Amount = result[i].Amount + amount[i].Amount * value[i].Value
                };
            }
        }
        return result.ToImmutable();
    }
}

public record StockValue(DateOnly Date, decimal Value);
public record OwnedStockAmount(DateOnly Date, decimal Amount);
public record OwnedStockValue(DateOnly Date, decimal Amount);