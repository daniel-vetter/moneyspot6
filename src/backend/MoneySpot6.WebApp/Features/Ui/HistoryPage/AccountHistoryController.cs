using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MoneySpot6.WebApp.Database;
using MoneySpot6.WebApp.Features.Ui.Shared;

namespace MoneySpot6.WebApp.Features.Ui.HistoryPage;

[ApiController]
[Route("api/[controller]")]
public class AccountHistoryController : Controller
{
    private readonly BalanceProvider _balanceProvider;
    private readonly StockDataProvider _stockDataProvider;
    private readonly Db _db;

    public AccountHistoryController(BalanceProvider balanceProvider, StockDataProvider stockDataProvider, Db db)
    {
        _balanceProvider = balanceProvider;
        _stockDataProvider = stockDataProvider;
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<ImmutableArray<AccountHistoryBalanceResponse>>> Get()
    {
        var minTransactionDate = await _db.BankAccountTransactions.Select(x => (DateOnly?)x.Final.Date).MinAsync();
        var minStockDate = await _db.StockTransactions.Select(x => (DateOnly?)x.Date).MinAsync();

        DateOnly startDate;
        if (minTransactionDate.HasValue && minStockDate.HasValue)
            startDate = minTransactionDate.Value < minStockDate.Value ? minTransactionDate.Value : minStockDate.Value;
        else if (minTransactionDate.HasValue)
            startDate = minTransactionDate.Value;
        else if (minStockDate.HasValue)
            startDate = minStockDate.Value;
        else
            return ImmutableArray<AccountHistoryBalanceResponse>.Empty;

        var endDate = DateOnly.FromDateTime(DateTime.Now);

        var balanceHistory = await _balanceProvider.GetBalanceHistory(startDate, endDate);
        var stockHistory = await _stockDataProvider.GetDailyOwnedStockValue(startDate, endDate);

        if (balanceHistory.Start != stockHistory.Start || balanceHistory.End != stockHistory.End)
            throw new Exception("Length does not match.");

        var r = ImmutableArray.CreateBuilder<AccountHistoryBalanceResponse>();
        for (var date = balanceHistory.Start; date < balanceHistory.End; date = date.AddDays(1))
        {
            r.Add(new AccountHistoryBalanceResponse
            {
                Date = date,
                Balance = balanceHistory[date],
                StockValue = stockHistory[date].EndOfDay.CurrentValue,
                StockInvested = stockHistory[date].EndOfDay.InvestedValue
            });
        }
        return r.ToImmutableArray();
    }
}
    
[PublicAPI]
public record AccountHistoryBalanceResponse
{
    [Required] public DateOnly Date { get; init; }
    [Required] public decimal Balance { get; set; }
    [Required] public decimal StockValue { get; set; }
    [Required] public decimal StockInvested { get; set; }
};