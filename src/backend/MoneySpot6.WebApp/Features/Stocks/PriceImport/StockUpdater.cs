using Microsoft.EntityFrameworkCore;
using MoneySpot6.WebApp.Database;
using MoneySpot6.WebApp.Features.Stocks.PriceImport.YahooAdapter;

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
            .AsNoTracking()
            .ToArrayAsync();

        foreach (var stock in allStocks)
        {
            ct.ThrowIfCancellationRequested();
            
            await RunImport(stock.Id, StockPriceInterval.Daily);
            await RunImport(stock.Id, StockPriceInterval.FiveMinutes);
        }
    }

    private async Task RunImport(int stockId, StockPriceInterval interval)
    {
        using var activity = AppActivitySource.Start("StockUpdate " + interval);
        
        var stock = await _db
            .Stocks
            .AsTracking()
            .Where(x => x.Id == stockId)
            .SingleAsync();

        if (stock.Symbol == null)
            return;

        Exception? error = null;
        try
        {
            var lastEntry = await _db.StockPrices
                .Where(x => x.Stock == stock)
                .Where(x => x.Interval == interval)
                .OrderByDescending(x => x.Timestamp)
                .FirstOrDefaultAsync();

            var queryStart = (interval, lastEntry?.Timestamp) switch
            {
                (StockPriceInterval.Daily, null) => new DateTimeOffset(2009, 1, 1, 0, 0, 0, TimeSpan.Zero),
                (StockPriceInterval.Daily, not null) => lastEntry.Timestamp.AddDays(-10),
                (StockPriceInterval.FiveMinutes, null) => DateTimeOffset.UtcNow.AddDays(-29),
                (StockPriceInterval.FiveMinutes, not null) => lastEntry.Timestamp.AddHours(-2),
                _ => throw new ArgumentOutOfRangeException()
            };
            var queryEnd = DateTimeOffset.UtcNow.AddDays(1);

            var prices = await _yahooStockDateProvider.Get(queryStart, queryEnd, stock.Symbol, interval);

            if (prices.Length == 0)
                return;
            
            var existingEntries = await _db.StockPrices
                .AsTracking()
                .Where(x => x.Stock == stock)
                .Where(x => x.Interval == interval)
                .Where(x => x.Timestamp >= queryStart && x.Timestamp <= queryEnd)
                .ToDictionaryAsync(x => x.Timestamp, x => x);

            var changedEntries = 0;
            var addedEntries = 0;
            foreach (var stockPrice in prices)
            {
                if (existingEntries.TryGetValue(stockPrice.Timestamp, out var existing))
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
                        Timestamp = stockPrice.Timestamp,
                        Interval = interval,
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
            _logger.LogInformation("Integrated new prices for stock {name} (Id: {id}, Interval: {interval}). Retrieved: {retrieved}, Changed: {changed}, Added: {added}", stock.Name, stock.Id, interval.ToString(), prices.Length, changedEntries, addedEntries);
        }
        catch (TaskCanceledException) { }
        catch (Exception e)
        {
            error = e;
            _logger.LogError(e, "Integration of new prices for stock {name} (Id: {id}, Interval: {interval}) failed", stock.Name, stock.Id, interval.ToString());
        }

        switch (interval)
        {
            case StockPriceInterval.Daily:
                stock.LastImportDaily = DateTimeOffset.UtcNow;
                stock.LastImportErrorDaily = error?.Message;
                break;
            case StockPriceInterval.FiveMinutes:
                stock.LastImport5Min = DateTimeOffset.UtcNow;
                stock.LastImportError5Min = error?.Message;
                break;
            default:
                throw new ArgumentException("Invalid interval");
        }
    }
}