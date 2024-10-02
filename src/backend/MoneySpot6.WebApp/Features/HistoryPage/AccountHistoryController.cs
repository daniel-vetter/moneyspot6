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

        public AccountHistoryController(Db db, BalanceProvider balanceProvider)
        {
            _db = db;
            _balanceProvider = balanceProvider;
        }

        [HttpGet]
        public async Task<ActionResult<ImmutableArray<AccountHistoryBalanceResponse>>> Get(
            [BindRequired, FromQuery] int[] accountIds,
            [BindRequired, JsonSchema(JsonObjectType.String, Format = "date-only")] DateOnly startDate,
            [BindRequired, JsonSchema(JsonObjectType.String, Format = "date-only")] DateOnly endDate)
        {
            var max = DateOnly.FromDateTime(DateTime.Now);
            if (startDate > max) startDate = max;
            if (endDate > max) endDate = max;

            var history = await _balanceProvider.GetBalanceHistory(startDate, endDate);

            return Ok(history.Select(x => new AccountHistoryBalanceResponse
            {
                Date = x.Date,
                Balance = x.Balance
            }).ToImmutableArray());
        }
    }

    public record AccountHistoryBalanceResponse
    {
        [Required] public DateOnly Date { get; init; }
        [Required] public long Balance { get; set; }
    };
}