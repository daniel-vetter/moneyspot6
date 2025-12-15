using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MoneySpot6.WebApp.Database;
using MoneySpot6.WebApp.Features.Ui.Shared;
using MoneySpot6.WebApp.Infrastructure;

namespace MoneySpot6.WebApp.Features.Ui.SummaryPage;

[ApiController]
[Route("api/[controller]")]
public class SummaryPageController : Controller
{
    private readonly Db _db;
    private readonly StockDataProvider _stockDataProvider;
    private readonly BalanceProvider _balanceProvider;

    public SummaryPageController(Db db, StockDataProvider stockDataProvider, BalanceProvider balanceProvider)
    {
        _db = db;
        _stockDataProvider = stockDataProvider;
        _balanceProvider = balanceProvider;
    }

    [HttpGet("GetBankAccountSummary")]
    public async Task<ActionResult<BankAccountSummaryResponse>> GetBankAccountSummary()
    {
        var entries = await _db.BankAccounts.Select(x => new BankAccountEntrySummaryResponse
            {
                Id = x.Id,
                Name = $"{x.Name} ({x.Type})",
                Total = x.Balance
            })
            .ToArrayAsync();

        return Ok(new BankAccountSummaryResponse
        {
            Total = entries.Aggregate(0m, (a, b) => a + b.Total),
            Accounts = [..entries]
        });
    }

    [HttpGet("GetCurrentMonthBalanceHistory")]
    public async Task<ActionResult<BalanceHistoryResponse>> GetCurrentMonthBalanceHistory()
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var startDate = today.AddDays(-31);

        var history = await _balanceProvider.GetBalanceHistory(startDate, today.AddDays(1));

        return Ok(new BalanceHistoryResponse
        {
            Entries = [..history.Select(x => new BalanceHistoryEntryResponse { Date = x.Key, Balance = x.Value })]
        });
    }

    [HttpGet("GetStockValueHistory")]
    public async Task<ActionResult<StockValueHistoryResponse>> GetStockValueHistory()
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var startDate = today.AddDays(-31);

        var history = await _stockDataProvider.GetDailyOwnedStockValue(startDate, today.AddDays(1));

        return Ok(new StockValueHistoryResponse
        {
            Entries = [..history.Select(x => new StockValueHistoryEntryResponse { Date = x.Key, Value = x.Value.EndOfDay.CurrentValue })]
        });
    }

    [HttpGet("GetStockSummary")]
    public async Task<ActionResult<StockSummaryResponse>> GetStockSummary()
    {
        var stocks = await _db.Stocks.AsNoTracking().ToArrayAsync();
        var result = ImmutableArray.CreateBuilder<StockSummaryEntryResponse>();
        foreach (var stock in stocks)
        {
            var currentPrice = (await _db.StockPrices
                .AsNoTracking()
                .Where(x => x.Stock.Id == stock.Id)
                .Where(x => x.Interval == StockPriceInterval.FiveMinutes)
                .OrderByDescending(x => x.Timestamp)
                .FirstOrDefaultAsync())?.Close ?? 0;

            var currentAmount = await _db.StockTransactions
                .AsNoTracking()
                .Where(x => x.Stock.Id == stock.Id)
                .Select(x => x.Amount)
                .SumAsync();
                
            result.Add(new StockSummaryEntryResponse
            {
                Id = stock.Id,
                Name = stock.Name,
                StockPrice = currentPrice,
                Total = currentPrice * currentAmount
            });
        }

        return Ok(new StockSummaryResponse
        {
            Entries = result.ToImmutable(),
            Total = result.Aggregate(0m, (a, b) => a + b.Total)
        });
    }
    
    [HttpGet("GetMonthSummary")]
    [Produces<MonthSummaryResponse[]>]
    public async Task<IActionResult> GetMonthSummary(int startMonth, int endMonth)
    {
        DateOnly ConvertToDateOnly(int month) => new(month / 12, month % 12 + 1, 1);
        
        if (endMonth < startMonth)
            return BadRequest("Invalid date range");
        
        var totalStartMonth = ConvertToDateOnly(startMonth);
        var totalEndMonth = ConvertToDateOnly(endMonth).AddMonths(1);

        var allTransactions = await _db.BankAccountTransactions
            .AsNoTracking()
            .Where(x => x.Final.Date >= totalStartMonth && x.Final.Date < totalEndMonth)
            .Select(x => x.Final)
            .ToImmutableArrayAsync();
        
        var allTransactionsByMonth = allTransactions
            .GroupBy(x => x.Date.Year * 12 + x.Date.Month - 1)
            .ToImmutableDictionary(x => x.Key, x => x.ToArray());
        
        var categories = await _db.Categories
            .AsNoTracking()
            .ToDictionaryAsync(x => x.Id, x => x);
        
        DbCategory? GetRootCategory(int? id)
        {
            if (id == null) return null;
            var parentId = categories[id.Value].ParentId;
            return parentId.HasValue 
                ? GetRootCategory(parentId.Value) 
                : categories[id.Value];
        }

        var result = ImmutableArray.CreateBuilder<MonthSummaryResponse>();
        for (var curMonth = startMonth; curMonth <= endMonth; curMonth++)
        {
            var transactionOfCurrentMonth = allTransactionsByMonth.GetValueOrDefault(curMonth) ?? [];

            var accountBalance = transactionOfCurrentMonth
                .Select(x => x.Amount)
                .Aggregate(0m, (a, b) => a + b);

            var totalByCategory = transactionOfCurrentMonth
                .GroupBy(x => GetRootCategory(x.CategoryId))
                .Select(x => new MonthSummaryCategoryResponse
                {
                    Name = x.Key?.Name ?? "Sonstiges",
                    Total = x.Select(y => y.Amount).Aggregate(0m, (a, b) => a + b)
                })
                .OrderByDescending(x => Math.Abs(x.Total))
                .ToImmutableArray();
        
            var stockData = await _stockDataProvider.GetDailyOwnedStockValue(
                ConvertToDateOnly(curMonth), 
                ConvertToDateOnly(curMonth).AddMonths(1).AddDays(1)
            );
            var stockValueStart = stockData.Values[0].StartOfDay.CurrentValue;
            var stockValueEnd =  stockData.Values.Last().StartOfDay.CurrentValue;
            var stockBalance = stockValueEnd - stockValueStart;

            var entry = new MonthSummaryResponse
            {
                Month = curMonth,
                AccountBalance = accountBalance,
                StockBalance = stockBalance,
                Categories = totalByCategory
            };
            result.Add(entry);
        }

        return Ok(result.ToImmutable());
    }
}

[PublicAPI]
public record StockSummaryResponse
{
    [Required] public required decimal Total { get; init; }
    [Required] public required ImmutableArray<StockSummaryEntryResponse> Entries { get; init; }
}

[PublicAPI]
public record StockSummaryEntryResponse
{
    [Required] public required int Id { get; init; }
    [Required] public required string Name { get; init; }
    [Required] public required decimal StockPrice { get; init; }
    [Required] public required decimal Total { get; init; }
}

[PublicAPI]
public record BankAccountEntrySummaryResponse
{
    [Required] public required int Id { get; init; }
    [Required] public required string Name { get; init; }
    [Required] public required decimal Total { get; init; }
}

[PublicAPI]
public record BankAccountSummaryResponse
{
    [Required] public required ImmutableArray<BankAccountEntrySummaryResponse> Accounts { get; init; }
    [Required] public required decimal Total { get; init; }
}

[PublicAPI]
public record MonthSummaryResponse
{
    [Required] public required int Month { get; init; }
    [Required] public required decimal AccountBalance { get; init; }
    [Required] public required decimal StockBalance { get; set; }
    [Required] public required ImmutableArray<MonthSummaryCategoryResponse>  Categories { get; init; }
}

[PublicAPI]
public record MonthSummaryCategoryResponse
{
    [Required] public required string Name { get; init; }
    [Required] public required decimal Total { get; init; }
}

[PublicAPI]
public record BalanceHistoryResponse
{
    [Required] public required ImmutableArray<BalanceHistoryEntryResponse> Entries { get; init; }
}

[PublicAPI]
public record BalanceHistoryEntryResponse
{
    [Required] public required DateOnly Date { get; init; }
    [Required] public required decimal Balance { get; init; }
}

[PublicAPI]
public record StockValueHistoryResponse
{
    [Required] public required ImmutableArray<StockValueHistoryEntryResponse> Entries { get; init; }
}

[PublicAPI]
public record StockValueHistoryEntryResponse
{
    [Required] public required DateOnly Date { get; init; }
    [Required] public required decimal Value { get; init; }
}