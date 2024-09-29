using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using MoneySpot6.WebApp.Database;
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

        public AccountHistoryController(Db db)
        {
            _db = db;
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
            
            // Create the result array with an balance of 0
            var resultList = ImmutableArray.CreateBuilder<AccountHistoryBalanceResponse>();
            for (var cur = startDate; cur < endDate; cur = cur.AddDays(1))
            {
                resultList.Add(new AccountHistoryBalanceResponse
                {
                    Date = cur,
                    Balance = 0
                });
            }
            var resultIndex = resultList.ToDictionary(x => x.Date, x => x);

            // Go through each requested account and add the balance
            var accounts = await _db.BankAccounts
                .Where(x => accountIds.Contains(x.Id))
                .AsNoTracking()
                .ToArrayAsync();

            foreach (var account in accounts)
            {
                var startBalance = await _db.BankAccountTransactions
                    .Where(x => x.BankAccount.Id == account.Id && x.Raw.Date < startDate)
                    .OrderByDescending(x => x.Raw.Date)
                    .ThenByDescending(x => x.Id)
                    .Select(x => (long?)x.Raw.NewBalance)
                    .FirstOrDefaultAsync();

                var balanceChanges = (await _db.BankAccountTransactions
                    .Where(x => x.BankAccount.Id == account.Id && x.Raw.Date >= startDate && x.Raw.Date < endDate)
                    .OrderBy(x => x.Raw.Date)
                    .ThenBy(x => x.Id)
                    .Select(x => new
                    {
                        x.Raw.Date,
                        x.Raw.NewBalance
                    })
                    .ToArrayAsync())
                    .GroupBy(x => x.Date)
                    .ToDictionary(x => x.Key, x => x.Last().NewBalance);

                var balance = startBalance ?? 0L;
                for (var cur = startDate; cur < endDate; cur = cur.AddDays(1))
                {
                    if (balanceChanges.TryGetValue(cur, out var balanceOfThisDay))
                        balance = balanceOfThisDay;

                    resultIndex[cur].Balance += balance;
                }
            }

            return Ok(resultList.ToImmutable());
        }
    }

    public record AccountHistoryBalanceResponse
    {
        [Required] public DateOnly Date { get; init; }
        [Required] public long Balance { get; set; }
    };
}