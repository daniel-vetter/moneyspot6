using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MoneySpot6.WebApp.Database;
using MoneySpot6.WebApp.Features.AccountSync.Services;
using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;

namespace MoneySpot6.WebApp.Features.TransactionPage;

[ApiController]
[Route("api/[controller]")]
public class TransactionPageController : Controller
{
    private readonly Db _db;
    private readonly TransactionDetailsCalculator _transactionDetailsCalculator;

    public TransactionPageController(Db db, TransactionDetailsCalculator transactionDetailsCalculator)
    {
        _db = db;
        _transactionDetailsCalculator = transactionDetailsCalculator;
    }

    [HttpGet]
    public async Task<ActionResult<TransactionResponse>> GetTransactions(string? search)
    {
        var categories = await _db.Categories
            .AsNoTracking()
            .ToDictionaryAsync(x => x.Id, x => x.Name);

        IQueryable<DbBankAccountTransaction> query = _db.BankAccountTransactions
            .AsNoTracking()
            .Include(x => x.BankAccount)
            .OrderByDescending(x => x.Raw.Date)
            .ThenByDescending(x => x.Id);

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(x => EF.Functions.ILike(x.Parsed.Purpose!, "%" + search + "%") || EF.Functions.ILike(x.Parsed.Name!, "%" + search + "%"));

        var entries = await query.Select(x => new
        {
            x.Id,
            x.Final.Date,
            x.Final.Name,
            x.Final.Purpose,
            x.Final.CategoryId,
            x.Final.Amount
        }).ToArrayAsync();

        var r = new TransactionResponse
        {
            Entries = [..entries.Select(x => new TransactionEntryResponse
            {
                Id = x.Id,
                Date = x.Date,
                Name = x.Name,
                Purpose = x.Purpose,
                CategoryName = x.CategoryId.HasValue && categories.TryGetValue(x.CategoryId.Value, out var catName) ? catName : null,
                Amount = x.Amount
            })]
        };

        return Ok(r);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<TransactionDetailsResponse>> Get(int id)
    {
        var entry = await _db.BankAccountTransactions
            .AsNoTracking()
            .Include(x => x.BankAccount)
            .Where(x => x.Id == id)
            .Select(x => new TransactionDetailsResponse
            {
                Id = x.Id,
                BaseDetails = new TransactionBaseDetails
                {
                    Date = x.Parsed.Date,
                    Purpose = x.Parsed.Purpose,
                    Name = x.Parsed.Name,
                    BankCode = x.Parsed.BankCode,
                    AccountNumber = x.Parsed.AccountNumber,
                    Iban = x.Parsed.Iban,
                    Bic = x.Parsed.Bic,
                    Amount = x.Parsed.Amount,
                    CategoryId = x.Parsed.CategoryId,
                    EndToEndReference = x.Parsed.EndToEndReference,
                    CustomerReference = x.Parsed.CustomerReference,
                    MandateReference = x.Parsed.MandateReference,
                    CreditorIdentifier = x.Parsed.CreditorIdentifier,
                    OriginatorIdentifier = x.Parsed.OriginatorIdentifier,
                    AlternateInitiator = x.Parsed.AlternateInitiator,
                    AlternateReceiver = x.Parsed.AlternateReceiver,
                    PaymentProcessor = x.Parsed.PaymentProcessor
                },
                OverriddenDetails = new TransactionOverrideDetails
                {
                    Date = x.Overridden.Date,
                    Purpose = x.Overridden.Purpose,
                    Name = x.Overridden.Name,
                    BankCode = x.Overridden.BankCode,
                    AccountNumber = x.Overridden.AccountNumber,
                    Iban = x.Overridden.Iban,
                    Bic = x.Overridden.Bic,
                    Amount = x.Overridden.Amount,
                    CategoryId = x.Overridden.CategoryId,
                    EndToEndReference = x.Overridden.EndToEndReference,
                    CustomerReference = x.Overridden.CustomerReference,
                    MandateReference = x.Overridden.MandateReference,
                    CreditorIdentifier = x.Overridden.CreditorIdentifier,
                    OriginatorIdentifier = x.Overridden.OriginatorIdentifier,
                    AlternateInitiator = x.Overridden.AlternateInitiator,
                    AlternateReceiver = x.Overridden.AlternateReceiver,
                    PaymentProcessor = x.Overridden.PaymentProcessor
                },
                Note = x.Note
            }).FirstOrDefaultAsync();
        if (entry == null) return NotFound();
        return Ok(entry);
    }

    [HttpPost]
    public async Task<ActionResult> Update(TransactionDetailsUpdateRequest update)
    {
        var entry = await _db.BankAccountTransactions
            .AsTracking()
            .Include(x => x.BankAccount)
            .Where(x => x.Id == update.Id)
            .SingleOrDefaultAsync();

        if (entry == null)
            return NotFound();

        entry.Overridden = new DbBankAccountTransactionOverrideData
        {
            Date = update.OverriddenDetails.Date,
            Purpose = update.OverriddenDetails.Purpose,
            Name = update.OverriddenDetails.Name,
            BankCode = update.OverriddenDetails.BankCode,
            AccountNumber = update.OverriddenDetails.AccountNumber,
            Iban = update.OverriddenDetails.Iban,
            Bic = update.OverriddenDetails.Bic,
            Amount = update.OverriddenDetails.Amount,
            CategoryId = update.OverriddenDetails.CategoryId,
            EndToEndReference = update.OverriddenDetails.EndToEndReference,
            CustomerReference = update.OverriddenDetails.CustomerReference,
            MandateReference = update.OverriddenDetails.MandateReference,
            CreditorIdentifier = update.OverriddenDetails.CreditorIdentifier,
            OriginatorIdentifier = update.OverriddenDetails.OriginatorIdentifier,
            AlternateInitiator = update.OverriddenDetails.AlternateInitiator,
            AlternateReceiver = update.OverriddenDetails.AlternateReceiver,
            PaymentProcessor = update.OverriddenDetails.PaymentProcessor
        };
        entry.Note = update.Note;
        entry.Final = _transactionDetailsCalculator.GetFinal(entry.Parsed, entry.Overridden);

        await _db.SaveChangesAsync();
        return Ok();
    }
}

public record TransactionDetailsUpdateRequest
{
    [Required] public required int Id { get; init; }
    [Required] public required TransactionOverrideDetails OverriddenDetails { get; init; }
    [Required(AllowEmptyStrings = true)] public required string Note { get; init; }
}

public record TransactionDetailsResponse
{
    [Required] public required int Id { get; init; }
    [Required] public required TransactionBaseDetails BaseDetails { get; init; }
    [Required] public required TransactionOverrideDetails OverriddenDetails { get; init; }
    [Required] public required string Note { get; init; }
}

public class TransactionBaseDetails
{
    public DateOnly Date { get; set; }
    public string Purpose { get; set; } = "";
    public string Name { get; set; } = "";
    public string BankCode { get; set; } = "";
    public string AccountNumber { get; set; } = "";
    public string Iban { get; set; } = "";
    public string Bic { get; set; } = "";
    public decimal Amount { get; set; }
    public int? CategoryId { get; set; }
    public string EndToEndReference { get; set; } = "";
    public string CustomerReference { get; set; } = "";
    public string MandateReference { get; set; } = "";
    public string CreditorIdentifier { get; set; } = "";
    public string OriginatorIdentifier { get; set; } = "";
    public string AlternateInitiator { get; set; } = "";
    public string AlternateReceiver { get; set; } = "";
    public PaymentProcessor PaymentProcessor { get; set; } = PaymentProcessor.None;

}

public class TransactionOverrideDetails
{
    public DateOnly? Date { get; set; }
    public string? Purpose { get; set; }
    public string? Name { get; set; }
    public string? BankCode { get; set; }
    public string? AccountNumber { get; set; }
    public string? Iban { get; set; }
    public string? Bic { get; set; }
    public decimal? Amount { get; set; }
    public int? CategoryId { get; set; }
    public string? EndToEndReference { get; set; }
    public string? CustomerReference { get; set; }
    public string? MandateReference { get; set; }
    public string? CreditorIdentifier { get; set; }
    public string? OriginatorIdentifier { get; set; }
    public string? AlternateInitiator { get; set; }
    public string? AlternateReceiver { get; set; }
    public PaymentProcessor? PaymentProcessor { get; set; }
}

public record TransactionResponse
{
    public required ImmutableArray<TransactionEntryResponse> Entries { get; init; }
}

public record TransactionEntryResponse
{
    public required int Id { get; init; }
    public required DateOnly Date { get; init; }
    public required string? Name { get; init; }
    public required string? Purpose { get; init; }
    public required string? CategoryName { get; init; }
    public required decimal Amount { get; init; }
}