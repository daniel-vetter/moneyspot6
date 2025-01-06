using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using MoneySpot6.WebApp.Database;
using MoneySpot6.WebApp.Features.Shared;
using NJsonSchema;
using NJsonSchema.Annotations;
using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;

namespace MoneySpot6.WebApp.Features.HistoryPage
{
    [ApiController]
    [Route("api/[controller]")]
    public class AccountHistoryController : Controller
    {
        private readonly Db _db;
        private readonly BalanceProvider _balanceProvider;
        private readonly StockDataProvider _stockDataProvider;

        public AccountHistoryController(Db db, BalanceProvider balanceProvider, StockDataProvider stockDataProvider)
        {
            _db = db;
            _balanceProvider = balanceProvider;
            _stockDataProvider = stockDataProvider;
        }

        [HttpGet]
        public async Task<ActionResult<ImmutableArray<AccountHistoryBalanceResponse>>> Get(
            [BindRequired, FromQuery] int[] accountIds,
            [BindRequired, JsonSchema(JsonObjectType.String, Format = "date-only")] DateOnly startDate,
            [BindRequired, JsonSchema(JsonObjectType.String, Format = "date-only")] DateOnly endDate)
        {
            var max = DateOnly.FromDateTime(DateTime.Now).AddDays(1);
            if (startDate > max) startDate = max;
            if (endDate > max) endDate = max;

            var balanceHistory = await _balanceProvider.GetBalanceHistory(startDate, endDate);
            var stockHistory = await _stockDataProvider.GetDailyOwnedStockValue(startDate, endDate);

            if (balanceHistory.Length != stockHistory.Length)
                throw new Exception("Length does not match.");

            var r = ImmutableArray.CreateBuilder<AccountHistoryBalanceResponse>();
            for (var i = 0; i < balanceHistory.Length; i++)
            {
                r.Add(new AccountHistoryBalanceResponse
                {
                    Date = balanceHistory[i].Date,
                    Balance = balanceHistory[i].Balance,
                    StockValue = (long)(stockHistory[i].CurrentValue * 100m), //TODO
                    StockInvested = (long)(stockHistory[i].InvestedValue * 100m) //TODO
                });
            }
            return r.ToImmutableArray();
        }
    }
    
    public record AccountHistoryBalanceResponse
    {
        [Required] public DateOnly Date { get; init; }
        [Required] public long Balance { get; set; }
        [Required] public long StockValue { get; set; }
        [Required] public long StockInvested { get; set; }
    };
}