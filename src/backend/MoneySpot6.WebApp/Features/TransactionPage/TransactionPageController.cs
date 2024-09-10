using System.Collections.Immutable;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MoneySpot6.WebApp.Database;

namespace MoneySpot6.WebApp.Features.TransactionPage
{
    [ApiController]
    [Route("api/[controller]")]
    public class TransactionPageController : Controller
    {
        private readonly Db _db;

        public TransactionPageController(Db db)
        {
            _db = db;
        }

        [HttpGet]
        public async Task<ActionResult<TransactionResponse>> GetTransactions(DateOnly startDate, DateOnly endDate)
        {
            var entries = await _db.BankAccountTransactions
                .OrderByDescending(x => x.RawData.Date)
                .ThenByDescending(x => x.Id)
                .Where(x => x.RawData.Date >= startDate && x.RawData.Date < endDate)
                .Select(x => new TransactionEntryResponse
                {
                    Id = x.Id,
                    Date = x.RawData.Date,
                    Purpose = x.RawData.Usage,
                    Value = x.RawData.Amount
                })
                .ToArrayAsync();

            var r = new TransactionResponse
            {
                Total = entries.Aggregate<TransactionEntryResponse, long>(0, (a, b) => a + b.Value),
                Expense = -entries.Where(x => x.Value < 0).Aggregate<TransactionEntryResponse, long>(0, (a, b) => a + b.Value),
                Income = entries.Where(x => x.Value > 0).Aggregate<TransactionEntryResponse, long>(0, (a, b) => a + b.Value),
                Entries = [..entries]
            };

            return Ok(r);
        }
    }

    public record TransactionResponse
    {
        public required ImmutableArray<TransactionEntryResponse> Entries { get; init; }
        public long Total { get; init; }
        public long Income { get; init; }
        public long Expense { get; init; }
    }

    public record TransactionEntryResponse
    {
        public required int Id { get; init; }
        public required DateOnly Date { get; init; }
        public required string Purpose { get; init; }
        public required long Value { get; init; }
    }
}