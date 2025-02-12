using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MoneySpot6.WebApp.Database;
using System.Collections.Immutable;

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
        public async Task<ActionResult<TransactionResponse>> GetTransactions(string? search)
        {
            IQueryable<DbBankAccountTransaction> query = _db.BankAccountTransactions
                .AsNoTracking()
                .Include(x => x.BankAccount)
                .OrderByDescending(x => x.Raw.Date)
                .ThenByDescending(x => x.Id);

            if (!string.IsNullOrWhiteSpace(search)) 
                query = query.Where(x => EF.Functions.ILike(x.Parsed.Purpose!, "%" + search + "%") || EF.Functions.ILike(x.Parsed.Name!, "%" + search + "%"));

            var entries = await query.Select(x => new TransactionEntryResponse
                {
                    Id = x.Id,
                    Icon = x.BankAccount.Icon,
                    IconColor = x.Parsed.PaymentProcessor == PaymentProcessor.Paypal ? "#009cde" : null,
                    Date = x.Raw.Date,
                    Name = x.Parsed.Name,
                    Purpose = x.Parsed.Purpose,
                    Value = x.Raw.Amount
                })
                .ToArrayAsync();

            var r = new TransactionResponse
            {
                Entries = [..entries]
            };

            return Ok(r);
        }
    }

    public record TransactionResponse
    {
        public required ImmutableArray<TransactionEntryResponse> Entries { get; init; }
    }

    public record TransactionEntryResponse
    {
        public required int Id { get; init; }
        public required string? Icon { get; init; }
        public required string? IconColor { get; init; }
        public required DateOnly Date { get; init; }
        public required string? Name { get; init; }
        public required string? Purpose { get; init; }
        public required decimal Value { get; init; }
    }
}