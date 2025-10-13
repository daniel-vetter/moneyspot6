using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
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

    public AccountHistoryController(BalanceProvider balanceProvider, StockDataProvider stockDataProvider)
    {
        _balanceProvider = balanceProvider;
        _stockDataProvider = stockDataProvider;
    }

    [HttpGet]
    public async Task<ActionResult<ImmutableArray<AccountHistoryBalanceResponse>>> Get(
        [BindRequired, JsonSchema(JsonObjectType.String, Format = "date-only")] DateOnly startDate,
        [BindRequired, JsonSchema(JsonObjectType.String, Format = "date-only")] DateOnly endDate)
    {
        var max = DateOnly.FromDateTime(DateTime.Now).AddDays(1);
        if (startDate > max) startDate = max;
        if (endDate > max) endDate = max;

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
    
public record AccountHistoryBalanceResponse
{
    [Required] public DateOnly Date { get; init; }
    [Required] public decimal Balance { get; set; }
    [Required] public decimal StockValue { get; set; }
    [Required] public decimal StockInvested { get; set; }
};