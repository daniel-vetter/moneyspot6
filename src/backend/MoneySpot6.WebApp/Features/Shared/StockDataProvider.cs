using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MoneySpot6.WebApp.Database;
using MoneySpot6.WebApp.Features.StockTransactions;
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
    public async Task<ImmutableArray<StockValue>> GetStockValueDailyHistory(int stockId, DateOnly start, DateOnly end)
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
    /// Returns the amount and price (when bought) of a stock over a time range.
    /// Result contains an entry for each day.
    /// </summary>
    public async Task<ImmutableArray<BoughtStock>> GetOwnershipDailyHistory(int stockId, DateOnly start, DateOnly end)
    {
        var startDto = new DateOnly(start.Year, start.Month, start.Day);
        var endDto = new DateOnly(end.Year, end.Month, end.Day);

        var cur = await _db.StockTransactions
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
        
        var curAmount = cur?.AmountSum ?? 0;
        var curPrice = cur?.PriceSum ?? 0;

        var stockTransactions = (await _db.StockTransactions
                .AsNoTracking()
                .Where(x => x.Stock.Id == stockId)
                .Where(x => x.Date >= startDto && x.Date < endDto)
                .ToImmutableArrayAsync())
            .GroupBy(x => x.Date)
            .ToImmutableDictionary(x => x.Key, x => new
            {
                Amount = x.Sum(y => y.Amount), 
                Price = x.Sum(y => y.Price * y.Amount)
            });

        var result = ImmutableArray.CreateBuilder<BoughtStock>();
        for (var curDate = start; curDate < end; curDate = curDate.AddDays(1))
        {
            if (stockTransactions.TryGetValue(curDate, out var changeAtDay))
            {
                curAmount += changeAtDay.Amount;
                curPrice += changeAtDay.Price;
            }
            
            result.Add(new BoughtStock(curDate, curAmount, curPrice));
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
            result.Add(new OwnedStockValue(curDay, 0, 0));

        foreach (var stockId in stockIds)
        {
            var owned = await GetOwnershipDailyHistory(stockId, start, end);
            var value = await GetStockValueDailyHistory(stockId, start, end);

            if (owned.Length != result.Count || value.Length != result.Count)
                throw new Exception("Length of lists is not equal.");

            for (var i = 0; i < owned.Length; i++)
            {
                result[i] = result[i] with
                {
                    CurrentValue = result[i].CurrentValue + owned[i].Amount * value[i].Value,
                    InvestedValue = result[i].InvestedValue + owned[i].PriceBoughtFor
                };
            }
        }
        return result.ToImmutable();
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

public record StockValue(DateOnly Date, decimal Value);
public record BoughtStock(DateOnly Date, decimal Amount, decimal PriceBoughtFor);
public record OwnedStockValue(DateOnly Date, decimal CurrentValue, decimal InvestedValue);