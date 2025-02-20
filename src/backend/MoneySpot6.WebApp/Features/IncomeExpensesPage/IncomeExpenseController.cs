using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using MoneySpot6.WebApp.Database;
using MoneySpot6.WebApp.Features.Shared;
using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;

namespace MoneySpot6.WebApp.Features.IncomeExpensesPage
{
    [ApiController]
    [Route("api/[controller]")]
    public class IncomeExpenseController : Controller
    {
        private readonly Db _db;
        private readonly StockDataProvider _stockDataProvider;

        public IncomeExpenseController(Db db, StockDataProvider stockDataProvider)
        {
            _db = db;
            _stockDataProvider = stockDataProvider;
        }

        [HttpGet("GetMonthlyIncomeAndExpenses")]
        public async Task<ActionResult<ImmutableArray<IncomeExpenseEntryResponse>>> Get(string? search, [BindRequired] IncomeExpenseGrouping grouping)
        {
            var groups = await _db
                .BankAccountTransactions
                .Where(x => string.IsNullOrWhiteSpace(search) || (EF.Functions.ILike(x.Parsed.Purpose!, "%" + search + "%") || EF.Functions.ILike(x.Parsed.Name!, "%" + search + "%")))
                .GroupBy(x =>
                    grouping == IncomeExpenseGrouping.None ? 0 :
                    grouping == IncomeExpenseGrouping.Year ? x.Raw.Date.Year * 13 :
                    x.Raw.Date.Year * 13 + x.Raw.Date.Month)
                .Select(x => new
                {
                    GroupKey = x.Key,
                    Income = x.Sum(y => Math.Max(0, y.Raw.Amount)),
                    Expense = -x.Sum(y => Math.Min(0, y.Raw.Amount)),
                })
                .OrderBy(x => x.GroupKey)
                .ToArrayAsync();

            if (groups.Length == 0)
                return Ok(ImmutableArray<IncomeExpenseEntryResponse>.Empty);

            var (stockQueryStart, stockQueryEnd) = GetMinMaxGroupDate(groups.Select(x => x.GroupKey).Min(), groups.Select(x => x.GroupKey).Max());
            var stocks = await _stockDataProvider.GetDailyOwnedStockValue(stockQueryStart, stockQueryEnd.AddDays(1));

            var result = groups.Select(x =>
                {
                    var (groupStart, groupEnd) = GetGroupStartAndEnd(x.GroupKey, stockQueryStart, stockQueryEnd);

                    return new IncomeExpenseEntryResponse
                    {
                        Month = x.GroupKey,
                        Income = x.Income,
                        Expense = x.Expense,
                        StockBalance = stocks[groupEnd].StartOfDay.CurrentValue - stocks[groupStart].StartOfDay.CurrentValue
                    };
                })
            .ToArray();

            return Ok(result);
        }

        private (DateOnly groupStart, DateOnly groupEnd) GetGroupStartAndEnd(int groupKey, DateOnly minDate, DateOnly maxDate)
        {
            if (groupKey == 0) return (minDate, maxDate);
            var y = groupKey / 13;
            var m = groupKey % 13;
            return m == 0
                ? (new DateOnly(y, 1, 1), new DateOnly(y, 1, 1).AddYears(1))
                : (new DateOnly(y, m, 1), new DateOnly(y, m, 1).AddMonths(1));
        }

        private (DateOnly minDate, DateOnly maxDate) GetMinMaxGroupDate(int minGroupKey, int maxGroupKey)
        {
            var (min, _) = GetGroupStartAndEnd(minGroupKey, new DateOnly(2009, 1, 1), DateOnly.MaxValue);
            var (_, max) = GetGroupStartAndEnd(maxGroupKey, DateOnly.MinValue, new DateOnly(DateTimeOffset.Now.Year + 1, 1, 1));
            return (min, max);
        }
    }

    public enum IncomeExpenseGrouping
    {
        None,
        Month,
        Year
    }

    public record IncomeExpenseEntryResponse
    {
        [Required] public int Month { get; set; }
        [Required] public decimal Income { get; set; }
        [Required] public decimal Expense { get; set; }
        [Required] public decimal StockBalance { get; set; }
    }
}