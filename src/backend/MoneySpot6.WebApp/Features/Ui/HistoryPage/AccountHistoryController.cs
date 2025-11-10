using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MoneySpot6.WebApp.Database;
using MoneySpot6.WebApp.Features.Ui.Shared;
using NJsonSchema;
using NJsonSchema.Annotations;

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
    public async Task<ActionResult<ImmutableArray<AccountHistoryBalanceResponse>>> Get(
        [JsonSchema(JsonObjectType.String, Format = "date-only")] DateOnly? startDate,
        [JsonSchema(JsonObjectType.String, Format = "date-only")] DateOnly? endDate)
    {
        if (startDate is null)
        {
            var minDate1 = await _db.BankAccountTransactions.Select(x => x.Final.Date).MinAsync();
            var minDate2 = await _db.StockTransactions.Select(x => x.Date).MinAsync();
            startDate = minDate1 < minDate2 ? minDate1 : minDate2;
        }

        endDate ??= DateOnly.FromDateTime(DateTime.Now);
        
        var max = DateOnly.FromDateTime(DateTime.Now).AddDays(1);
        if (startDate > max) startDate = max;
        if (endDate > max) endDate = max;

        var balanceHistory = await _balanceProvider.GetBalanceHistory(startDate.Value, endDate.Value);
        var stockHistory = await _stockDataProvider.GetDailyOwnedStockValue(startDate.Value, endDate.Value);

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