using System.Collections.Immutable;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MoneySpot6.WebApp.Database;

namespace MoneySpot6.WebApp.Features.SummaryPage
{
    [ApiController]
    [Route("api/[controller]")]
    public class SummaryPageController : Controller
    {
        private readonly Db _db;

        public SummaryPageController(Db db)
        {
            _db = db;
        }

        [HttpGet("GetBackAccountSummary")]
        public async Task<ActionResult<BankAccountSummaryResponse>> GetBackAccountSummary()
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
                Entries = [..entries]
            });
        }
    }

    public record BankAccountEntrySummaryResponse
    {
        public required int Id { get; init; }
        public required string Name { get; init; }
        public required long Total { get; init; }
    }

    public record BankAccountSummaryResponse
    {
        public required ImmutableArray<BankAccountEntrySummaryResponse> Entries { get; init; }
        public required long Total { get; init; }
    }
}
