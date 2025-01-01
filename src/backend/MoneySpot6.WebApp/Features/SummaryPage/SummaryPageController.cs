using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MoneySpot6.WebApp.Database;
using MoneySpot6.WebApp.Features.Shared;
using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;

namespace MoneySpot6.WebApp.Features.SummaryPage
{
    [ApiController]
    [Route("api/[controller]")]
    public class SummaryPageController : Controller
    {
        private readonly Db _db;
        private readonly BalanceProvider _balanceProvider;

        public SummaryPageController(Db db, BalanceProvider balanceProvider)
        {
            _db = db;
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
                Total = entries.Aggregate(0L, (a, b) => a + b.Total),
                Accounts = [..entries]
            });
        }

        [HttpGet("GetBankAccountGoal")]
        public async Task<ActionResult<BankAccountTotalGoalResponse>> GetBankAccountGoal()
        {
            var startDate = new DateOnly(2024, 09, 01);
            var targetDate = new DateOnly(2025, 06, 01);
            var targetBalance = 60_000_00;
            var currentBalance = await _balanceProvider.GetCurrentBalance();
            var startBalance = await _balanceProvider.GetBalanceAtStartOf(startDate);
            var actualHistory = await _balanceProvider.GetBalanceHistory(new DateOnly(2024, 09, 01), DateOnly.FromDateTime(DateTime.Now).AddDays(1));

            var change = targetBalance - startBalance;
            var totalDayCount = (targetDate.ToDateTime(TimeOnly.MinValue) - startDate.ToDateTime(TimeOnly.MinValue)).TotalDays;
            var expected = ImmutableArray.CreateBuilder<BalanceEntryResponse>();
            for (var cur = startDate; cur <= targetDate; cur = cur.AddDays(1))
            {
                var percentage = expected.Count / totalDayCount;
                var value = startBalance + change * percentage;
                expected.Add(new BalanceEntryResponse(cur, (long)value));
            }

            var remainingMoney = targetBalance - currentBalance;
            var remainingDays = (targetDate.ToDateTime(TimeOnly.MinValue) - DateTime.Today).TotalDays;
            var requiredSavingPerDay = remainingMoney / remainingDays;

            return Ok(new BankAccountTotalGoalResponse
            {
                EndBalance = targetBalance,
                EndDate = targetDate,
                RequiredSavingPerMonth = (long)(requiredSavingPerDay * 30),
                ActualHistory = [..actualHistory.Select(x => new BalanceEntryResponse(x.Date, x.Balance))],
                ExpectedHistory = expected.ToImmutable()
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
    }

    public record StockSummaryResponse
    {
        [Required] public required decimal Total { get; init; }
        [Required] public required ImmutableArray<StockSummaryEntryResponse> Entries { get; init; }
    }

    public record StockSummaryEntryResponse
    {
        [Required] public long Id { get; init; }
        [Required] public string Name { get; init; }
        [Required] public decimal StockPrice { get; init; }
        [Required] public decimal Total { get; init; }
    }

    public record BankAccountEntrySummaryResponse
    {
        [Required] public required int Id { get; init; }
        [Required] public required string Name { get; init; }
        [Required] public required long Total { get; init; }
    }

    public record BankAccountTotalGoalResponse
    {
        [Required] public DateOnly EndDate { get; init; }
        [Required] public long EndBalance { get; init; }
        [Required] public long RequiredSavingPerMonth { get; init; }
        [Required] public ImmutableArray<BalanceEntryResponse> ActualHistory { get; init; }
        [Required] public ImmutableArray<BalanceEntryResponse> ExpectedHistory { get; init; }
    }

    public record BalanceEntryResponse([property:Required] DateOnly Date, [property:Required] long Balance);

    public record BankAccountSummaryResponse
    {
        [Required] public required ImmutableArray<BankAccountEntrySummaryResponse> Accounts { get; init; }
        [Required] public required long Total { get; init; }
    }
}
