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
                Total = entries.Length == 0 ? 0 : entries.Sum(x => x.Total),
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
            var actualHistory = await _balanceProvider.GetBalanceHistory(new DateOnly(2024, 09, 01), DateOnly.FromDateTime(DateTime.Now));

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
    }

    public record BankAccountEntrySummaryResponse
    {
        public required int Id { get; init; }
        public required string Name { get; init; }
        public required long Total { get; init; }
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
