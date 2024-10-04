using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using MoneySpot6.WebApp.Database;

namespace MoneySpot6.WebApp.Features.IncomeExpensesPage
{
    [ApiController]
    [Route("api/[controller]")]
    public class IncomeExpenseController : Controller
    {
        private readonly Db _db;

        public IncomeExpenseController(Db db)
        {
            _db = db;
        }

        [HttpGet("GetMonthlyIncomeAndExpenses")]
        public async Task<ActionResult<ImmutableArray<IncomeExpenseEntryResponse>>> Get([BindRequired] IncomeExpenseGrouping grouping)
        {
            var result = await _db
                .BankAccountTransactions
                .GroupBy(x => new
                {
                    x.Raw.Date.Year,
                    Month = grouping == IncomeExpenseGrouping.Month ? x.Raw.Date.Month : (int?)null
                }).Select(x => new
                {
                    x.Key.Year,
                    x.Key.Month,
                    Income = x.Sum(y => Math.Max(0, y.Raw.Amount)),
                    Expense = -x.Sum(y => Math.Min(0, y.Raw.Amount))
                })
            .OrderBy(x => x.Year)
            .ThenBy(x => x.Month)
                .Select(x => new IncomeExpenseEntryResponse
                {
                    Year = x.Year,
                    Month = x.Month,
                    Income = x.Income,
                    Expense = x.Expense
                })
            .ToArrayAsync();

            return Ok(result);
        }
    }

    public enum IncomeExpenseGrouping
    {
        Month,
        Year
    }

    public record IncomeExpenseEntryResponse
    {
        [Required] public int Year { get; set; }
        public int? Month { get; set; }
        [Required] public long Income { get; set; }
        [Required] public long Expense { get; set; }
    }
}
