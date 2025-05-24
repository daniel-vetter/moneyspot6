using System.Collections.Immutable;
using Microsoft.EntityFrameworkCore;
using MoneySpot6.WebApp.Database;

namespace MoneySpot6.WebApp.Features.Shared;

[ScopedService]
public class PortfolioProvider
{
    private readonly StockDataProvider _stockDataProvider;
    private readonly Db _db;

    public PortfolioProvider(StockDataProvider stockDataProvider, Db db)
    {
        _stockDataProvider = stockDataProvider;
        _db = db;
    }

    public async Task<ImmutableArray<PortfolioStock>> GetPortfolio()
    {
        var dbTransactions = await _db.StockTransactions
            .AsNoTracking()
            .Include(x => x.Stock)
            .ToArrayAsync();

        var groupedByStock= dbTransactions
            .GroupBy(x => new
            {
                StockId = x.Stock.Id,
                StockName = x.Stock.Name
            })
            .Select(x => new
            {
                x.Key.StockId,
                x.Key.StockName,
                Transactions = x.OrderBy(x => x.Date).ThenBy(x => x.Id).ToImmutableArray()
            });

        var stockPrices = await _stockDataProvider.GetStockPrices();
        var result = ImmutableArray.CreateBuilder<PortfolioStock>();
        foreach (var stock in groupedByStock)
        {
            var resultEntriesOfCurrentStock = ImmutableArray.CreateBuilder<PortfolioStockPurchases>();
            var queue = new Queue<PortfolioStockPurchases>();
            foreach (var transaction in stock.Transactions)
            {
                if (transaction.Amount >= 0)
                {
                    var entry = new PortfolioStockPurchases
                    {
                        BuyTransactionId = transaction.Id,
                        Date = transaction.Date,
                        PurchaseAmount = transaction.Amount,
                        PurchasePrice = transaction.Price * transaction.Amount,
                        SoldAmount = 0,
                        SoldPrice = 0,
                        SoldTax = 0,
                        RemainingAmount = transaction.Amount,
                        RemainingPrice = transaction.Price * transaction.Amount,
                        RemainingTax = 0
                    };
                    queue.Enqueue(entry);
                    resultEntriesOfCurrentStock.Add(entry);
                }
                else
                {
                    var toRemove = -transaction.Amount;
                    while (toRemove > 0)
                    {
                        if (!queue.TryPeek(out var buy))
                            throw new Exception("Failed to create report");

                        var soldFromThisEntry = Math.Min(toRemove, buy.SoldAmount);
                        toRemove -= soldFromThisEntry;

                        buy.SoldAmount += soldFromThisEntry;
                        buy.SoldPrice += soldFromThisEntry * transaction.Price;
                        buy.SoldTax += GetTax(buy.PurchasePrice, transaction.Price * soldFromThisEntry);

                        if (buy.SoldAmount >= buy.PurchaseAmount)
                            queue.Dequeue();
                    }
                }
            }

            foreach (var entry in resultEntriesOfCurrentStock)
            {
                if (!stockPrices.TryGetValue(stock.StockId, out var currentPrice))
                    throw new Exception("No price for stock " + stock.StockId + " found.");

                entry.RemainingAmount = entry.PurchaseAmount - entry.SoldAmount;
                entry.RemainingPrice = entry.RemainingAmount * currentPrice;
                entry.RemainingTax = GetTax(entry.PurchasePrice, entry.RemainingAmount * currentPrice);
            }

            result.Add(new PortfolioStock
            {
                StockId = stock.StockId,
                StockName = stock.StockName,
                PurchasePrice = resultEntriesOfCurrentStock.Sum(x => x.PurchasePrice),
                PurchaseAmount = resultEntriesOfCurrentStock.Sum(x => x.PurchaseAmount),
                SoldPrice = resultEntriesOfCurrentStock.Sum(x => x.SoldAmount),
                SoldAmount = resultEntriesOfCurrentStock.Sum(x => x.SoldAmount),
                SoldTax = resultEntriesOfCurrentStock.Sum(x => x.SoldTax),
                RemainingPrice = resultEntriesOfCurrentStock.Sum(x => x.RemainingPrice),
                RemainingAmount = resultEntriesOfCurrentStock.Sum(x => x.RemainingAmount),
                RemainingTax = resultEntriesOfCurrentStock.Sum(x => x.RemainingTax),
                Purchases = resultEntriesOfCurrentStock.ToImmutable()
            });
        }

        return result.ToImmutable();
    }

    private decimal GetTax(decimal buyPrice, decimal sellPrice)
    {
        if (buyPrice > sellPrice)
            return 0;

        var profit = sellPrice - buyPrice;
        return profit * 0.25m;  //TODO
    }
}

public record PortfolioStock
{
    public required int StockId { get; set; }
    public required string StockName { get; set; }
    public required decimal PurchaseAmount { get; set; }
    public required decimal PurchasePrice { get; set; }
    public required decimal SoldAmount { get; set; }
    public required decimal SoldPrice { get; set; }
    public required decimal SoldTax { get; set; }
    public required decimal RemainingAmount { get; set; }
    public required decimal RemainingPrice { get; set; }
    public required decimal RemainingTax { get; set; }
    public required ImmutableArray<PortfolioStockPurchases> Purchases { get; set; }
}

public record PortfolioStockPurchases
{
    public int BuyTransactionId { get; init; }
    public required DateOnly Date { get; set; }
    public required decimal PurchaseAmount { get; set; }
    public required decimal PurchasePrice { get; set; }
    public required decimal SoldAmount { get; set; }
    public required decimal SoldPrice { get; set; }
    public required decimal SoldTax { get; set; }
    public required decimal RemainingAmount { get; set; }
    public required decimal RemainingPrice { get; set; }
    public required decimal RemainingTax { get; set; }
}