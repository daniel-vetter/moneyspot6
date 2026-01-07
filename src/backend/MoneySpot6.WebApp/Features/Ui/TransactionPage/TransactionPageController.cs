using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MoneySpot6.WebApp.Database;
using MoneySpot6.WebApp.Features.Core.TransactionProcessing;
using MoneySpot6.WebApp.Features.Core.Inflation;
using NJsonSchema;
using NJsonSchema.Annotations;

namespace MoneySpot6.WebApp.Features.Ui.TransactionPage;

[ApiController]
[Route("api/[controller]")]
public class TransactionPageController : Controller
{
    private readonly Db _db;
    private readonly TransactionProcessingFacade _transactionProcessingFacade;
    private readonly InflationCalculator _inflationCalculator;

    public TransactionPageController(Db db, TransactionProcessingFacade transactionProcessingFacade, InflationCalculator inflationCalculator)
    {
        _db = db;
        _transactionProcessingFacade = transactionProcessingFacade;
        _inflationCalculator = inflationCalculator;
    }

    [HttpGet]
    public async Task<ActionResult<TransactionResponse>> GetTransactions(
        string? search,
        [JsonSchema(JsonObjectType.String, Format = "date-only")] DateOnly? startDate,
        [JsonSchema(JsonObjectType.String, Format = "date-only")] DateOnly? endDate,
        [JsonSchema(JsonObjectType.String, Format = "date-only")] DateOnly? inflationAdjustmentDate)
    {
        var categories = await _db.Categories
            .AsNoTracking()
            .ToDictionaryAsync(x => x.Id, x => x.Name);

        IQueryable<DbBankAccountTransaction> query = _db.BankAccountTransactions
            .AsNoTracking()
            .OrderByDescending(x => x.Raw.Date)
            .ThenByDescending(x => x.Id);

        if (startDate != null)
            query = query.Where(x => x.Final.Date >= startDate);
        
        if (endDate != null)
            query = query.Where(x => x.Final.Date < endDate);
            
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(x => EF.Functions.ILike(x.Final.Purpose, "%" + search + "%") || EF.Functions.ILike(x.Final.Name, "%" + search + "%"));

        var entries = query.Select(x => new
        {
            x.Id,
            x.Final.Date,
            x.Final.Name,
            x.Final.Purpose,
            x.Final.CategoryId,
            x.Final.Amount,
            x.Final.TransactionType,
            x.IsNew,
        }).AsAsyncEnumerable();

        if (inflationAdjustmentDate.HasValue)
        {
            await _inflationCalculator.EnsureConfigIsLoaded();
        }

        var b = ImmutableArray.CreateBuilder<TransactionEntryResponse>();
        await foreach (var x in entries)
        {
            var amount = x.Amount;

            if (inflationAdjustmentDate.HasValue)
            {
                amount = _inflationCalculator.CalculateInflationAdjustedValue(x.Amount, x.Date, inflationAdjustmentDate.Value);
            }

            b.Add(new TransactionEntryResponse
            {
                Id = x.Id,
                Date = x.Date,
                Name = x.Name,
                Purpose = x.Purpose,
                CategoryName = x.CategoryId.HasValue && categories.TryGetValue(x.CategoryId.Value, out var catName) ? catName : null,
                Amount = amount,
                IsNew = x.IsNew,
                TransactionType = x.TransactionType
            });
        }

        return Ok(new TransactionResponse
        {
            Entries = b.ToImmutable()
        });
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
                    Date = x.Processed.Date ?? x.Parsed.Date,
                    Purpose = x.Processed.Purpose ?? x.Parsed.Purpose,
                    Name = x.Processed.Name ?? x.Parsed.Name,
                    BankCode = x.Processed.BankCode ?? x.Parsed.BankCode,
                    AccountNumber = x.Processed.AccountNumber ?? x.Parsed.AccountNumber,
                    Iban = x.Processed.Iban ?? x.Parsed.Iban,
                    Bic = x.Processed.Bic ?? x.Parsed.Bic,
                    Amount = x.Processed.Amount ?? x.Parsed.Amount,
                    CategoryId = x.Processed.CategoryId ?? x.Parsed.CategoryId,
                    EndToEndReference = x.Processed.EndToEndReference ?? x.Parsed.EndToEndReference,
                    CustomerReference = x.Processed.CustomerReference ?? x.Parsed.CustomerReference,
                    MandateReference = x.Processed.MandateReference ?? x.Parsed.MandateReference,
                    CreditorIdentifier = x.Processed.CreditorIdentifier ?? x.Parsed.CreditorIdentifier,
                    OriginatorIdentifier = x.Processed.OriginatorIdentifier ?? x.Parsed.OriginatorIdentifier,
                    AlternateInitiator = x.Processed.AlternateInitiator ?? x.Parsed.AlternateInitiator,
                    AlternateReceiver = x.Processed.AlternateReceiver ?? x.Parsed.AlternateReceiver,
                    PaymentProcessor = x.Processed.PaymentProcessor ?? x.Parsed.PaymentProcessor,
                    TransactionType = x.Processed.TransactionType ?? x.Parsed.TransactionType
                },
                OverriddenDetails = new TransactionOverrideDetails
                {
                    Date = x.Overridden!.Date,
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
                    PaymentProcessor = x.Overridden.PaymentProcessor,
                    TransactionType = x.Overridden.TransactionType
                },
                Note = x.Note
            }).FirstOrDefaultAsync();
        if (entry == null) return NotFound();
        return Ok(entry);
    }

    [HttpGet("{id}/parsed-data")]
    public async Task<ActionResult<TransactionParsedDataResponse>> GetParsedData(int id)
    {
        var entry = await _db.BankAccountTransactions
            .AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new TransactionParsedDataResponse
            {
                Id = x.Id,
                Date = x.Parsed.Date,
                Purpose = x.Parsed.Purpose,
                Name = x.Parsed.Name,
                BankCode = x.Parsed.BankCode,
                AccountNumber = x.Parsed.AccountNumber,
                Iban = x.Parsed.Iban,
                Bic = x.Parsed.Bic,
                Amount = x.Parsed.Amount,
                EndToEndReference = x.Parsed.EndToEndReference,
                CustomerReference = x.Parsed.CustomerReference,
                MandateReference = x.Parsed.MandateReference,
                CreditorIdentifier = x.Parsed.CreditorIdentifier,
                OriginatorIdentifier = x.Parsed.OriginatorIdentifier,
                AlternateInitiator = x.Parsed.AlternateInitiator,
                AlternateReceiver = x.Parsed.AlternateReceiver,
                PaymentProcessor = x.Parsed.PaymentProcessor,
                TransactionType = x.Parsed.TransactionType
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
            PaymentProcessor = update.OverriddenDetails.PaymentProcessor,
            TransactionType = update.OverriddenDetails.TransactionType
        };
        entry.Note = update.Note;
        await _db.SaveChangesAsync();
        
        await _transactionProcessingFacade.UpdateTransactions([entry.Id]);
        return Ok();
    }

    [HttpPost("MarkAllSeen")]
    public async Task<bool> MarkAllSeen()
    {
        var changedEntries = await _db
            .BankAccountTransactions
            .Where(x => x.IsNew)
            .ExecuteUpdateAsync(x => x.SetProperty(y => y.IsNew, false));

        return changedEntries != 0;
    }
    
    [HttpGet("GetNewCount")]
    public async Task<int> GetNewCount()
    {
        var newCount = await _db
            .BankAccountTransactions
            .Where(x => x.IsNew)
            .CountAsync();

        return newCount;
    }
}

[PublicAPI]
public record TransactionDetailsUpdateRequest
{
    [Required] public required int Id { get; init; }
    [Required] public required TransactionOverrideDetails OverriddenDetails { get; init; }
    [Required(AllowEmptyStrings = true)] public required string Note { get; init; }
}

[PublicAPI]
public record TransactionDetailsResponse
{
    [Required] public required int Id { get; init; }
    [Required] public required TransactionBaseDetails BaseDetails { get; init; }
    [Required] public required TransactionOverrideDetails OverriddenDetails { get; init; }
    [Required] public required string Note { get; init; }
}

[PublicAPI]
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
    public TransactionType TransactionType { get; set; } = TransactionType.External;
}

[PublicAPI]
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
    public TransactionType? TransactionType { get; set; }
}

[PublicAPI]
public record TransactionParsedDataResponse
{
    [Required] public required int Id { get; init; }
    [Required] public required DateOnly Date { get; init; }
    [Required] public required string Purpose { get; init; }
    [Required] public required string Name { get; init; }
    [Required] public required string BankCode { get; init; }
    [Required] public required string AccountNumber { get; init; }
    [Required] public required string Iban { get; init; }
    [Required] public required string Bic { get; init; }
    [Required] public required decimal Amount { get; init; }
    [Required] public required string EndToEndReference { get; init; }
    [Required] public required string CustomerReference { get; init; }
    [Required] public required string MandateReference { get; init; }
    [Required] public required string CreditorIdentifier { get; init; }
    [Required] public required string OriginatorIdentifier { get; init; }
    [Required] public required string AlternateInitiator { get; init; }
    [Required] public required string AlternateReceiver { get; init; }
    [Required] public required PaymentProcessor PaymentProcessor { get; init; }
    [Required] public required TransactionType TransactionType { get; init; }
}

[PublicAPI]
public record TransactionResponse
{
    public required ImmutableArray<TransactionEntryResponse> Entries { get; init; }
}

[PublicAPI]
public record TransactionEntryResponse
{
    public required int Id { get; init; }
    public required DateOnly Date { get; init; }
    public required string? Name { get; init; }
    public required string? Purpose { get; init; }
    public required string? CategoryName { get; init; }
    public required decimal Amount { get; init; }
    public required bool IsNew { get; init; }
    public required TransactionType TransactionType { get; init; }
}