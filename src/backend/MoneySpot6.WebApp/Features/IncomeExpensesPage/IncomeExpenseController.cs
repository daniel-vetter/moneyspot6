using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using MoneySpot6.WebApp.Database;
using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;

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
        public async Task<ActionResult<ImmutableArray<IncomeExpenseEntryResponse>>> Get(string? search, [BindRequired] IncomeExpenseGrouping grouping)
        {
            IQueryable<DbBankAccountTransaction> query = _db.BankAccountTransactions;

            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(x => EF.Functions.ILike(x.Parsed.Purpose!, "%" + search + "%") || EF.Functions.ILike(x.Parsed.Name!, "%" + search + "%"));

            var result = await query.GroupBy(x => new
            {
                Year = grouping == IncomeExpenseGrouping.None ? (int?)null : x.Raw.Date.Year,
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
        None,
        Month,
        Year
    }

    public record IncomeExpenseEntryResponse
    {
        public int? Year { get; set; }
        public int? Month { get; set; }
        [Required] public long Income { get; set; }
        [Required] public long Expense { get; set; }
    }
}
